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
		public float ChangeRotationDuration = 0.25f;

		public VisualsShape FallingVisualsShape { get; private set; }

		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private int m_CurrentFallingColumn = 0;
		private bool m_ChangingFallingColumn = false;
		private bool m_ChangingFallingColumnByAnalog = false;

		private float m_ChangingFallingColumnStarted;
		private Quaternion m_ChangingFallingColumnStartedRotation;


		private bool m_RotatingFallingShape = false;
		private bool m_RotatingFallingShapeAnalog = false;
		private float m_RotatingFallingShapeStarted;
		private Vector3[] m_RotatingFallingShapeStartedScales;

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

			m_RotatingFallingShape = false;
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

		void Update()
		{
			if (LevelData == null)
				return;

#if UNITY_EDITOR
			VisualsGrid.__GizmoUpdateFallingColumn(LevelData.FallingColumn);
#endif

			UpdateFallingVisualsFacing();

			UpdateFallingVisualsPosition();
		}

		private void UpdateFallingVisualsFacing()
		{
			// Analog offset overrides other movement animations.
			if (TowerLevel.FallingColumnAnalogOffset != 0f) {
				m_CurrentFallingColumn = LevelData.FallingColumn;

				Quaternion endRotation = VisualsGrid.GridColumnToRotation(m_CurrentFallingColumn);
				var eulerAngles = endRotation.eulerAngles;
				eulerAngles.y -= TowerLevel.FallingColumnAnalogOffset * VisualsGrid.ConeSectorEulerAngle;
				endRotation.eulerAngles = eulerAngles;

				FallingVisualsContainer.localRotation = endRotation;

				m_ChangingFallingColumn = false;
				m_ChangingFallingColumnByAnalog = true;

				return;
			}

			if (m_CurrentFallingColumn != LevelData.FallingColumn || m_ChangingFallingColumnByAnalog) {
				m_ChangingFallingColumnStarted = Time.time;
				m_ChangingFallingColumnStartedRotation = FallingVisualsContainer.localRotation;
				m_ChangingFallingColumn = true;
				m_ChangingFallingColumnByAnalog = false;
				m_CurrentFallingColumn = LevelData.FallingColumn;
			}

			if (m_ChangingFallingColumn && Time.time - m_ChangingFallingColumnStarted > ChangeColumnDuration) {
				m_ChangingFallingColumn = false;

				FallingVisualsContainer.localRotation = VisualsGrid.GridColumnToRotation(m_CurrentFallingColumn);
			}

			if (m_ChangingFallingColumn) {
				float progress = (Time.time - m_ChangingFallingColumnStarted) / ChangeColumnDuration;
				Quaternion endRotation = VisualsGrid.GridColumnToRotation(m_CurrentFallingColumn);

				FallingVisualsContainer.localRotation = Quaternion.Lerp(m_ChangingFallingColumnStartedRotation, endRotation, progress);
			}
		}

		private void OnFallingShapeRotated()
		{
			if (m_RotatingFallingShapeAnalog)
				return;

			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			m_RotatingFallingShape = true;
			m_RotatingFallingShapeStarted = Time.time;

			m_RotatingFallingShapeStartedScales = new Vector3[FallingVisualsShape.ShapeCoords.Length];

			for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				ref var shapeCoords = ref FallingVisualsShape.ShapeCoords[i];
				shapeCoords.Coords = LevelData.FallingShape.ShapeCoords[i].Coords;
				m_RotatingFallingShapeStartedScales[i] = shapeCoords.Value.transform.localScale;
			}
		}

		private void UpdateFallingVisualsPosition()
		{
			if (FallingVisualsShape == null || TowerLevel.AreGridActionsRunning)
				return;

			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			// Analog offset overrides other rotate animations.
			if (TowerLevel.FallingShapeAnalogRotateOffset != 0f) {

				int fallingRows = LevelData.FallingShape.Rows;

				for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
					ref var shapeCoords = ref FallingVisualsShape.ShapeCoords[i];
					shapeCoords.Coords = LevelData.FallingShape.ShapeCoords[i].Coords;

					Vector3 currentScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);

					float previewRow = MathUtils.WrapValue(shapeCoords.Coords.Row + Mathf.Sign(TowerLevel.FallingShapeAnalogRotateOffset), fallingRows);
					Vector3 previewScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - previewRow);


					shapeCoords.Value.transform.localScale = Vector3.Lerp(currentScale, previewScale, Mathf.Abs(TowerLevel.FallingShapeAnalogRotateOffset));

					//float rowDistance = previewRow - TowerLevel.FallingShapeAnalogRotateOffset;
					//shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - rowDistance);
				}

				m_RotatingFallingShape = false;
				m_RotatingFallingShapeAnalog = true;

				return;
			}

			if (m_RotatingFallingShapeAnalog) {
				m_RotatingFallingShapeAnalog = false;
				OnFallingShapeRotated();
			}

			if (m_RotatingFallingShape && Time.time - m_RotatingFallingShapeStarted > ChangeRotationDuration) {
				m_RotatingFallingShape = false;

				for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
					ref var shapeCoords = ref FallingVisualsShape.ShapeCoords[i];
					shapeCoords.Coords = LevelData.FallingShape.ShapeCoords[i].Coords;
					shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
				}
			}

			if (m_RotatingFallingShape) {
				float progress = (Time.time - m_RotatingFallingShapeStarted) / ChangeRotationDuration;

				for(int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
					var shapeCoords = FallingVisualsShape.ShapeCoords[i];

					Vector3 endScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
					shapeCoords.Value.transform.localScale = Vector3.Lerp(m_RotatingFallingShapeStartedScales[i], endScale, progress);
				}

			} else {

				foreach(var shapeCoords in FallingVisualsShape.ShapeCoords) {
					shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
				}
			}
		}
	}

}