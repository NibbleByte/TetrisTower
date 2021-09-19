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
		// Tuple<Start Row, End Row, Wrapped>
		private Tuple<int, int, bool>[] m_RotatingFallingShapeCoordsChange;
		private float m_RotatingFallingShapeAnalogLastProgress;

		private void Awake()
		{
			TowerLevel.LevelInitialized += OnLevelInitialized;
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

			OnFallingShapeSelected();
		}

		private void OnPlacingFallingShape()
		{
			// Instead of destroying blocks now and re-creating them later, try reusing them.
			//DestroyFallingVisuals();
			VisualsGrid.SetPlacedShapeToBeReused(FallingVisualsShape);

			m_RotatingFallingShape = false;
			m_RotatingFallingShapeAnalog = false;
		}

		private void OnFallingShapeSelected()
		{
			DestroyFallingVisuals();

			if (LevelData.FallingShape != null) {
				CreateFallingVisuals();
			}
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
			if (!float.IsNaN(TowerLevel.FallingColumnAnalogOffset)) {
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

			m_RotatingFallingShapeCoordsChange = new Tuple<int, int, bool>[FallingVisualsShape.ShapeCoords.Length];

			for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				ref var shapeCoords = ref FallingVisualsShape.ShapeCoords[i];

				// Reset the position, interrupting any prior rotation.
				shapeCoords.Value.transform.position = VisualsGrid.ConeApex;
				shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);

				var targetCoords = LevelData.FallingShape.ShapeCoords[i].Coords;
				bool isWrapped = Mathf.Abs(targetCoords.Row - shapeCoords.Coords.Row) > 1;
				m_RotatingFallingShapeCoordsChange[i] = new Tuple<int, int, bool>(shapeCoords.Coords.Row, targetCoords.Row, isWrapped);

				shapeCoords.Coords = targetCoords;
			}
		}

		private void UpdateFallingVisualsPosition()
		{
			if (FallingVisualsShape == null || TowerLevel.AreGridActionsRunning)
				return;

			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			// Analog offset overrides other rotate animations.
			if (!float.IsNaN(TowerLevel.FallingShapeAnalogRotateOffset)) {

				int fallingRows = LevelData.FallingShape.Rows;

				if (m_RotatingFallingShapeCoordsChange?.Length != FallingVisualsShape.ShapeCoords.Length) {
					m_RotatingFallingShapeCoordsChange = new Tuple<int, int, bool>[FallingVisualsShape.ShapeCoords.Length];
				}

				for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
					ref var shapeCoords = ref FallingVisualsShape.ShapeCoords[i];
					shapeCoords.Coords = LevelData.FallingShape.ShapeCoords[i].Coords;

					// Configure data as if we are rotating towards the current row.
					// This will help a smooth transition when player releases drag and animation falls back to the current row.
					int nextRow = shapeCoords.Coords.Row;
					int startRow = MathUtils.WrapValue(nextRow + (int)Mathf.Sign(TowerLevel.FallingShapeAnalogRotateOffset), fallingRows);
					bool isWrapped = Mathf.Abs(nextRow - startRow) > 1;

					m_RotatingFallingShapeCoordsChange[i] = new Tuple<int, int, bool>(startRow, nextRow, isWrapped);
				}

				m_RotatingFallingShape = false;
				m_RotatingFallingShapeAnalog = true;
			}

			// Analog rotation finished.
			if (m_RotatingFallingShapeAnalog && float.IsNaN(TowerLevel.FallingShapeAnalogRotateOffset)) {
				m_RotatingFallingShapeAnalog = false;
				m_RotatingFallingShape = true;

				// Simulate normal rotation, from hotkey.
				m_RotatingFallingShapeStarted = Time.time - ChangeRotationDuration * m_RotatingFallingShapeAnalogLastProgress;
			}

			// Normal rotation finished.
			if (m_RotatingFallingShape && !m_RotatingFallingShapeAnalog && Time.time - m_RotatingFallingShapeStarted > ChangeRotationDuration) {
				m_RotatingFallingShape = false;
			}

			if (m_RotatingFallingShape || m_RotatingFallingShapeAnalog) {
				float progress = (Time.time - m_RotatingFallingShapeStarted) / ChangeRotationDuration;

				if (m_RotatingFallingShapeAnalog) {
					// 1f - offset simulates that progress is rotating towards the current (start) row, not the next one.
					// This will help a smooth transition when player releases drag and animation falls back to the current row.
					progress = 1f - Mathf.Abs(TowerLevel.FallingShapeAnalogRotateOffset);
					m_RotatingFallingShapeAnalogLastProgress = progress;
				}

				for (int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
					var shapeCoords = FallingVisualsShape.ShapeCoords[i];

					Vector3 startScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - m_RotatingFallingShapeCoordsChange[i].Item1);
					Vector3 endScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - m_RotatingFallingShapeCoordsChange[i].Item2);
					shapeCoords.Value.transform.localScale = Vector3.Lerp(startScale, endScale, progress);

					// Graph formula: min(8-abs(x / 0.0625 - 8), 2.5 / 2)
					// BlockDepth 2.5 is divided by two because both groups move in opposite directions.
					float zOffset = Mathf.Min(8 - Mathf.Abs(progress / 0.0625f - 8), VisualsGrid.BlockDepth / 2f);

					// Wrapped goes in front, others go behind.
					int sign = m_RotatingFallingShapeCoordsChange[i].Item3 ? 1 : -1;

					shapeCoords.Value.transform.position = VisualsGrid.ConeApex;
					shapeCoords.Value.transform.localPosition += new Vector3(0f, 0f, sign * zOffset);
				}

			} else {

				foreach(var shapeCoords in FallingVisualsShape.ShapeCoords) {
					shapeCoords.Value.transform.position = VisualsGrid.ConeApex;
					shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
				}
			}
		}
	}

}