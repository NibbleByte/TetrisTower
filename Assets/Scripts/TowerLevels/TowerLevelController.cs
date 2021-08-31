using System.Collections;
using System.Collections.Generic;
using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelController : MonoBehaviour
	{
		public TowerLevelData LevelData { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private TowerLevelData m_DebugLevelData;


		public List<GameGrid> Grids { get; private set; } = new List<GameGrid>();
		public BlocksGrid Grid => LevelData?.Grid;

		public bool IsPaused => !enabled;

		public event System.Action LevelInitialized;
		public event System.Action LevelFallingShapeChanged;
		public event System.Action PlacingFallingShape;
		public event System.Action PlacedOutsideGrid;
		public event System.Action FallingShapeSelected;

		public event System.Action FallingColumnChanged;
		public event System.Action FallingShapeRotated;

		private int m_NextRunId = 0;
		private int m_FinishedRuns = 0;

		public bool AreGridActionsRunning => m_FinishedRuns != m_NextRunId;

		public float FallingColumnAnalogOffset { get; private set; }

		private float m_FallingSpeedup = 0;

		public void Init(TowerLevelData data)
		{
			LevelData = m_DebugLevelData = data;

			if (LevelData.NextShape == null) {
				LevelData.NextShape = GenerateShape();
			}

			Grids.Clear();
			Grids.Add(LevelData.Grid);

			LevelInitialized?.Invoke();
			LevelFallingShapeChanged?.Invoke();
		}

		public IEnumerator RunActions(IList<GridAction> actions)
		{
			var currentRunId = m_NextRunId;
			m_NextRunId++;

			while (m_FinishedRuns < currentRunId)
				yield return null;


			while (actions.Count > 0) {

				foreach (var grid in Grids) {
					yield return grid.ApplyActions(actions);
				}

				actions = GameGridEvaluation.Evaluate(Grid, LevelData.Rules);
			}

			m_FinishedRuns++;
		}

		public void SelectFallingShape()
		{
			if (LevelData.NextShape == null) {
				LevelData.NextShape = GenerateShape();
			}

			LevelData.FallingShape = LevelData.NextShape;
			LevelData.FallDistanceNormalized = 0f;

			LevelData.NextShape = GenerateShape();

			if (!LevelData.Rules.WrapSidesOnMove) {
				if (LevelData.FallingColumn + LevelData.FallingShape.MaxColumn > Grid.Columns) {
					LevelData.FallingColumn = Grid.Columns - LevelData.FallingShape.MaxColumn - 1;
				}

				if (LevelData.FallingColumn + LevelData.FallingShape.MinColumn < 0) {
					LevelData.FallingColumn = 0 - LevelData.FallingShape.MinColumn;
				}
			}

			FallingShapeSelected?.Invoke();
		}

		public bool RequestFallingShapeMove(int offsetColumns)
		{
			int requestedColumn = LevelData.FallingColumn + offsetColumns;
			if (!LevelData.Rules.WrapSidesOnMove) {
				requestedColumn = Mathf.Clamp(requestedColumn,
					0 - (LevelData.FallingShape?.MinColumn ?? 0),
					Grid.Columns - (LevelData.FallingShape?.MaxColumn ?? 0) - 1
					);
			}

			// NOTE: This won't check in-between cells and jump over occupied ones.
			var fallCoords = LevelData.CalcFallShapeCoordsAt(requestedColumn);
			fallCoords.WrapAround(Grid);

			if (LevelData.FallingShape != null) {
				foreach (var pair in LevelData.FallingShape.AddToCoordsWrapped(fallCoords, Grid)) {

					if (pair.Coords.Row < Grid.Rows && Grid[pair.Coords])
						return false;
				}
			}

			LevelData.FallingColumn = Grid.WrappedColumn(requestedColumn);

			FallingColumnChanged?.Invoke();

			return true;
		}

		public bool AddFallingShapeAnalogMoveOffset(float offset)
		{
			FallingColumnAnalogOffset += offset;

			ValidateAnalogOffset();

			while (FallingColumnAnalogOffset > 0.5f) {
				FallingColumnAnalogOffset -= 1f;
				RequestFallingShapeMove(1);
			}

			while (FallingColumnAnalogOffset < -0.5f) {
				FallingColumnAnalogOffset += 1f;
				RequestFallingShapeMove(-1);
			}

			return true;
		}

		public bool ClearFallingShapeAnalogMoveOffset()
		{
			int roundDirection = Mathf.RoundToInt(FallingColumnAnalogOffset);

			FallingColumnAnalogOffset = 0f;

			// Snap to the closest.
			if (roundDirection != 0) {
				RequestFallingShapeMove(roundDirection);
			}

			return true;
		}

		private void ValidateAnalogOffset()
		{
			if (FallingColumnAnalogOffset != 0f) {

				// Check if falling shape can move to the next column or there are other shapes.
				int requestedColumn = LevelData.FallingColumn + (int)Mathf.Sign(FallingColumnAnalogOffset);
				if (!LevelData.Rules.WrapSidesOnMove) {
					requestedColumn = Mathf.Clamp(requestedColumn,
						0 - (LevelData.FallingShape?.MinColumn ?? 0),
						Grid.Columns - (LevelData.FallingShape?.MaxColumn ?? 0) - 1
						);
				}

				// NOTE: This won't check in-between cells and jump over occupied ones.
				var fallCoords = LevelData.CalcFallShapeCoordsAt(requestedColumn);
				fallCoords.WrapAround(Grid);

				if (LevelData.FallingShape != null) {
					foreach (var pair in LevelData.FallingShape.AddToCoordsWrapped(fallCoords, Grid)) {

						if (pair.Coords.Row < Grid.Rows && Grid[pair.Coords]) {
							ClearFallingShapeAnalogMoveOffset();
							break;
						}
					}
				}
			}
		}

		public bool RequestFallingSpeedUp(float speedUp)
		{
			if (LevelData.FallingShape == null)
				return false;

			m_FallingSpeedup = speedUp;

			return true;
		}

		public bool RequestFallingShapeRotate(int rotate)
		{
			if (LevelData.FallingShape == null)
				return false;

			int fallingRows = LevelData.FallingShape.Rows;

			for (int i = 0; i < LevelData.FallingShape.ShapeCoords.Length; ++i) {
				ref var shapeBind = ref LevelData.FallingShape.ShapeCoords[i];
				shapeBind.Coords.Row = MathUtils.WrapValue(shapeBind.Coords.Row + rotate, fallingRows);
			}

			FallingShapeRotated?.Invoke();

			return true;
		}

		private IEnumerator PlaceFallingShape(GridCoords placeCoords, BlocksShape placedShape)
		{
			var actions = new List<GridAction>() {
				new PlaceAction() { PlaceCoords = placeCoords, PlacedShape = placedShape }
			};

			yield return RunActions(actions);

			// Limit was reached, game over.
			if (placeCoords.Row + placedShape.Rows - 1 < Grid.Rows) {
				SelectFallingShape();
			} else {
				Debug.Log($"Placed shape with size ({placedShape.Rows}, {placedShape.Columns}) at {placeCoords}, but that goes outside the grid ({Grid.Rows}, {Grid.Columns}). It will be trimmed!");
				PlacedOutsideGrid?.Invoke();
			}
		}

		public BlocksShape GenerateShape()
		{
			GridShapeTemplate template = LevelData.ShapeTemplates[Random.Range(0, LevelData.ShapeTemplates.Length)];
			return GenerateShape(template, LevelData.SpawnedBlocks);
		}

		private static BlocksShape GenerateShape(GridShapeTemplate template, BlockType[] spawnBlocks)
		{
			var shapeCoords = new List<BlocksShape.ShapeBind>();
			foreach(var coords in template.ShapeTemplate) {
				var blockType = spawnBlocks[Random.Range(0, spawnBlocks.Length)];

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = coords,
					Value = blockType,
				});
			}

			return new BlocksShape() { ShapeCoords = shapeCoords.ToArray() };
		}

		public void PauseLevel()
		{
			enabled = false;
		}

		public void ResumeLevel()
		{
			enabled = true;
		}

		void Update()
		{
			// Wait for level to be initialized.
			if (LevelData == null)
				return;

			if (LevelData.FallingShape != null) {
				UpdateFallShape();

			} else if (!AreGridActionsRunning && CanSelectNextShape()) {
				SelectFallingShape();
			}

			DebugClearRow();
		}

		void LateUpdate()
		{
			ValidateAnalogOffset();
		}

		private bool CanSelectNextShape()
		{
			if (LevelData.NextShape == null)
				return false;

			// Search if the bottom of the next shape can be spawned.
			foreach (var shapeCoords in LevelData.NextShape.ShapeCoords) {
				if (shapeCoords.Coords.Row == 0) {

					var coords = new GridCoords() {
						Row = Grid.Rows - 1,
						Column = LevelData.FallingColumn + shapeCoords.Coords.Column
					};
					coords.WrapColumn(Grid);

					if (Grid[coords] != null) {
						return false;
					}
				}
			}

			return true;
		}

		private void UpdateFallShape()
		{
			// Clamp to avoid skipping over a cell on hiccups.
			LevelData.FallDistanceNormalized += Mathf.Clamp01(Time.deltaTime * (LevelData.FallSpeedNormalized + m_FallingSpeedup));

			var fallCoords = LevelData.FallShapeCoords;

			foreach (var pair in LevelData.FallingShape.AddToCoordsWrapped(fallCoords, Grid)) {

				if (pair.Coords.Row >= Grid.Rows)
					continue;

				if (pair.Coords.Row < 0 || Grid[pair.Coords]) {

					PlacingFallingShape?.Invoke();

					// The current coords are taken so move back one tile up where it should be free.
					fallCoords.Row++;

					m_FallingSpeedup = 0f;

					var fallShape = LevelData.FallingShape;
					LevelData.FallingShape = null;

					StartCoroutine(PlaceFallingShape(fallCoords, fallShape));
					break;
				}
			}
		}

		private void DebugClearRow()
		{
#if UNITY_EDITOR
			if (Keyboard.current == null)
				return;

			// Temporary disable.
			for (Key key = Key.Digit1; key <= Key.Digit0; ++key) {

				int keyRow = (key - Key.Digit1 + 1) % 10;

				if (Keyboard.current[key].wasPressedThisFrame && keyRow < Grid.Rows) {

					List<GridCoords> clearCoords = new List<GridCoords>();
					for (int column = 0; column < Grid.Columns; ++column) {
						var coords = new GridCoords(keyRow, column);
						if (Grid[coords]) {
							clearCoords.Add(coords);
						}
					}

					if (clearCoords.Count > 0) {
						var actions = new List<GridAction>() { new ClearMatchedAction() { Coords = clearCoords } };
						StartCoroutine(RunActions(actions));
					}
				}
			}
#endif
		}
	}
}