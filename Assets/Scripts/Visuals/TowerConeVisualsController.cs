using System;
using System.Collections.Generic;
using TetrisTower.TowerLevels;
using TetrisTower.Logic;
using UnityEngine;
using TetrisTower.Core;

namespace TetrisTower.Visuals
{
	public class TowerConeVisualsController : MonoBehaviour
	{
		[Serializable]
		public class VisualsShape : GridShape<GameObject>
		{
		}

		public ConeVisualsGrid VisualsGrid;
		public TowerLevelController TowerLevel;
		public TowerLevelData LevelData => TowerLevel.LevelData;

		public Transform FallingVisualsContainer;

		public float ChangeColumnDuration = 0.1f;

		public VisualsShape FallingVisualsShape { get; private set; }

		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private int m_CurrentFallingColumn = 0;
		private bool m_ChangingFallingColumn = false;
		private float m_ChangingFallingColumnStarted;
		private Quaternion m_ChangingFallingColumnStartedRotation;

		private void Awake()
		{
			TowerLevel.LevelInitialized += OnLevelInitialized;
			TowerLevel.LevelFallingShapeChanged += ReplaceFallingVisuals;
			TowerLevel.PlacingFallingShape += OnPlacingFallingShape;
			TowerLevel.PlacedOutsideGrid += DestroyFallingVisuals;
			TowerLevel.FallingShapeSelected += OnFallingShapeSelected;

			TowerLevel.FallingShapeRotated += OnFallingShapeRotated;

			FallingVisualsContainer.SetParent(transform);
			FallingVisualsContainer.localPosition = Vector3.zero;
		}

		private void OnLevelInitialized()
		{
			TowerLevel.Grids.Add(VisualsGrid);
			VisualsGrid.Init(LevelData.Grid);

			DestroyFallingVisuals();

			FallingVisualsContainer.localRotation = VisualsGrid.GridColumnToRotation(LevelData.FallingColumn);
			m_CurrentFallingColumn = LevelData.FallingColumn;
		}

		private void ReplaceFallingVisuals()
		{
			DestroyFallingVisuals();

			if (LevelData.FallingShape != null) {
				CreateFallingVisuals();
			}
		}

		private void OnPlacingFallingShape()
		{
			// Instead of destroying blocks now and re-creating them later, try reusing them.
			//DestroyFallingVisuals();
			VisualsGrid.SetPlacedShapeToBeReused(FallingVisualsShape);
		}

		private void OnFallingShapeSelected()
		{
			DestroyFallingVisuals();

			CreateFallingVisuals();
		}

		private void CreateFallingVisuals()
		{
			var blocksShape = LevelData.FallingShape;

			FallingVisualsShape = m_DebugFallingVisualsShape = new VisualsShape();

			var visualbinds = new List<GridShape<GameObject>.ShapeBind>();

			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab3D, FallingVisualsContainer);
				visualbinds.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));

				visualBlock.transform.position = VisualsGrid.ConeApex;
				visualBlock.transform.localRotation = VisualsGrid.GridColumnToRotation(bind.Coords.Column);	 // Set rotation in case of shape with multiple columns.
				visualBlock.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - bind.Coords.Row);
			}

			FallingVisualsShape.ShapeCoords = visualbinds.ToArray();
		}

		private void DestroyFallingVisuals()
		{
			if (FallingVisualsShape == null)
				return;

			foreach (var bind in FallingVisualsShape.ShapeCoords) {
				// Don't destroy if reused in the Visuals grid itself.
				// It could have been destroyed already by the visuals grid if match happened.
				if (bind.Value && bind.Value.transform.IsChildOf(FallingVisualsContainer)) {
					GameObject.Destroy(bind.Value.gameObject);
				}
			}

			FallingVisualsShape = m_DebugFallingVisualsShape = null;
		}

		private void OnFallingShapeRotated()
		{
			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			for(int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				FallingVisualsShape.ShapeCoords[i].Coords = LevelData.FallingShape.ShapeCoords[i].Coords;
			}
		}

		void Update()
		{
			if (LevelData == null)
				return;

#if UNITY_EDITOR
			VisualsGrid.__GizmoUpdateFallingColumn(LevelData.FallingColumn);
#endif

			UpdateRotation();

			if (FallingVisualsShape != null && !TowerLevel.AreGridActionsRunning) {

				foreach (var shapeCoords in FallingVisualsShape.ShapeCoords) {
					shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
				}
			}
		}

		private void UpdateRotation()
		{
			if (m_CurrentFallingColumn != LevelData.FallingColumn) {
				m_ChangingFallingColumnStarted = Time.time;
				m_ChangingFallingColumnStartedRotation = FallingVisualsContainer.localRotation;
				m_ChangingFallingColumn = true;
				m_CurrentFallingColumn = LevelData.FallingColumn;
			}

			if (m_ChangingFallingColumn && Time.time - m_ChangingFallingColumnStarted > ChangeColumnDuration) {
				m_ChangingFallingColumn = false;

				FallingVisualsContainer.localRotation = VisualsGrid.GridColumnToRotation(m_CurrentFallingColumn);
			}

			if (m_ChangingFallingColumn) {
				Quaternion endRotation = VisualsGrid.GridColumnToRotation(m_CurrentFallingColumn);

				FallingVisualsContainer.localRotation =
					Quaternion.Lerp(m_ChangingFallingColumnStartedRotation, endRotation, (Time.time - m_ChangingFallingColumnStarted) / ChangeColumnDuration);
			}
		}
	}

}