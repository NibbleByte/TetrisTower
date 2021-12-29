using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Logic
{
	public class GridLevelController : MonoBehaviour
	{
		public GridLevelData LevelData { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private GridLevelData m_DebugLevelData;


		public List<GameGrid> Grids { get; private set; } = new List<GameGrid>();
		public BlocksGrid Grid => LevelData?.Grid;

		public bool IsPaused => !enabled;

		public event Action RunningActionsSequenceFinished;
		public event Action SpawnBlockTypesCountChanged;
		public event Action PlacingFallingShape;
		public event Action PlacedOutsideGrid;
		public event Action FinishedLevel;
		public event Action FallingShapeSelected;

		public event Action FallingColumnChanged;
		public event Action FallingShapeRotated;

		private int m_NextRunId = 0;
		private int m_FinishedRuns = 0;

		public bool AreGridActionsRunning => m_FinishedRuns != m_NextRunId;

		public float FallingColumnAnalogOffset { get; private set; } = float.NaN;
		public float FallingShapeAnalogRotateOffset { get; private set; } = float.NaN;

		private float m_FallingSpeedup = 0;

		public void Init(GridLevelData data)
		{
			LevelData = m_DebugLevelData = data;

			if (LevelData.Score == null) {
				LevelData.Score = new ScoreGrid(LevelData.Grid.Rows, LevelData.Grid.Columns, LevelData.Rules);
			}

			if (LevelData.Rules.MatchScoring == 0) {
				Debug.LogError($"Initializing a game with no MatchScoring set.", this);
			}

			if (LevelData.PlayableSize.Column != LevelData.Grid.Columns) {
				Debug.LogError($"Playable size columns differ from the grid ones! Overwriting it!", this);

				LevelData.PlayableSize.Column = LevelData.Grid.Columns;
			}

			if (LevelData.Grid.Rows <= LevelData.PlayableSize.Row) {
				Debug.LogError($"Playable size rows is larger than grid ones! Overwriting it!", this);

				int extraRows = LevelData.ShapeTemplates.Max(st => st.Rows);
				LevelData.PlayableSize.Row = LevelData.Grid.Rows - extraRows;
			}

			if (LevelData.NextShape == null) {
				LevelData.NextShape = GenerateShape();
			}

			Grids.Clear();
			Grids.Add(LevelData.Grid);
			Grids.Add(LevelData.Score);
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

			// Mark the matching sequence as complete. Used for scoring etc.
			var finishedSequenceActions = new GridAction[] { new MatchingSequenceFinishAction() };
			foreach (var grid in Grids) {
				yield return grid.ApplyActions(finishedSequenceActions);
			}

			m_FinishedRuns++;

			RunningActionsSequenceFinished?.Invoke();

			int spawnBlockTypesProgress = LevelData.AddSpawnBlockTypePerMatches > 0
				? LevelData.Score.TotalClearedBlocksCount / LevelData.AddSpawnBlockTypePerMatches
				: 0
				;
			int spawnBlockTypesCount = LevelData.InitialSpawnBlockTypesCount + spawnBlockTypesProgress;

			if (LevelData.ClearBlocksRemainingCount == 0 && !AreGridActionsRunning) {
				Debug.Log($"Remaining blocks to clear are 0. Player won.");
				FinishLevel(TowerLevelRunningState.Won);

			} else if (LevelData.SpawnBlockTypesCount != spawnBlockTypesCount && LevelData.SpawnedBlocks.Length >= spawnBlockTypesCount) {
				Debug.Log($"Changing SpawnBlockTypesCount from {LevelData.SpawnBlockTypesCount} to {spawnBlockTypesCount}.", this);
				LevelData.SpawnBlockTypesCount = spawnBlockTypesCount;
				SpawnBlockTypesCountChanged?.Invoke();
			}
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
			if (float.IsNaN(FallingColumnAnalogOffset)) {
				FallingColumnAnalogOffset = 0;
			}

			FallingColumnAnalogOffset += offset;

			ValidateAnalogMoveOffset();

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
			if (float.IsNaN(FallingColumnAnalogOffset))
				return false;

			int roundDirection = Mathf.RoundToInt(FallingColumnAnalogOffset);

			FallingColumnAnalogOffset = float.NaN;

			// Snap to the closest.
			if (roundDirection != 0) {
				RequestFallingShapeMove(roundDirection);
			}

			return true;
		}

		private void ValidateAnalogMoveOffset()
		{
			if (!float.IsNaN(FallingColumnAnalogOffset)) {

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
							FallingColumnAnalogOffset = 0;
							break;
						}
					}
				}
			}
		}
		public bool AddFallingShapeAnalogRotateOffset(float offset)
		{
			if (float.IsNaN(FallingShapeAnalogRotateOffset)) {
				FallingShapeAnalogRotateOffset = 0;
			}

			FallingShapeAnalogRotateOffset += offset;

			// Since our shapes are 1x3 rotating them is always possible.
			// If we start using wide shapes, this needs to validate if rotation is possible.
			//ValidateAnalogRotateOffset();

			while (FallingShapeAnalogRotateOffset >= 0.5f) {
				//Debug.LogWarning($"ROTATION for {FallingShapeAnalogRotateOffset} ({offset}) at {Time.time} frame {Time.frameCount}");
				FallingShapeAnalogRotateOffset -= 1f;

				if (FallingShapeAnalogRotateOffset == -0.5f)
					FallingShapeAnalogRotateOffset += 0.0001f;

				RequestFallingShapeRotate(1);
			}

			while (FallingShapeAnalogRotateOffset <= -0.5f) {
				//Debug.LogWarning($"ROTATION for {FallingShapeAnalogRotateOffset} ({offset}) at {Time.time} frame {Time.frameCount}");
				FallingShapeAnalogRotateOffset += 1f;

				if (FallingShapeAnalogRotateOffset == 0.5f)
					FallingShapeAnalogRotateOffset -= 0.0001f;

				RequestFallingShapeRotate(-1);
			}

			return true;
		}

		public bool ClearFallingShapeAnalogRotateOffset()
		{
			if (float.IsNaN(FallingShapeAnalogRotateOffset))
				return false;

			int roundDirection = Mathf.RoundToInt(FallingShapeAnalogRotateOffset);

			FallingShapeAnalogRotateOffset = float.NaN;

			// Snap to the closest.
			if (roundDirection != 0) {
				RequestFallingShapeRotate(roundDirection);
			}

			return true;
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

			bool placedInside = placeCoords.Row <= LevelData.PlayableSize.Row; // '<=' as placing on the edge is a warning, not direct fail.
			placedInside &= placeCoords.Row + placedShape.Rows - 1 < Grid.Rows;

			// Limit was reached, game over.
			if (placedInside) {
				yield return RunActions(actions);
				SelectFallingShape();
			} else {

				Debug.Log($"Placed shape with size ({placedShape.Rows}, {placedShape.Columns}) at {placeCoords}, but that goes outside the play area {LevelData.PlayableSize}, grid ({Grid.Rows}, {Grid.Columns}).");

				while (AreGridActionsRunning)
					yield return null;

				m_NextRunId++;

				// Manually place the shape as we don't want to evaluate and execute normal game-play logic. Just stay visually there.
				foreach (var grid in Grids) {
					yield return grid.ApplyActions(actions);
				}

				m_FinishedRuns++;

				PlacedOutsideGrid?.Invoke();
				FinishLevel(TowerLevelRunningState.Lost);
			}
		}

		public BlocksShape GenerateShape()
		{
			GridShapeTemplate template = LevelData.ShapeTemplates[LevelData.Random.Next(LevelData.ShapeTemplates.Length)];
			return GenerateShape(template, new ArraySegment<BlockType>(LevelData.SpawnedBlocks, 0, LevelData.SpawnBlockTypesCount), LevelData.Random);
		}

		private static BlocksShape GenerateShape(GridShapeTemplate template, ArraySegment<BlockType> spawnBlocks, System.Random random)
		{
			var shapeCoords = new List<BlocksShape.ShapeBind>();
			foreach(var coords in template.ShapeTemplate) {
				var blockType = spawnBlocks.Array[spawnBlocks.Offset + random.Next(spawnBlocks.Count)];

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = coords,
					Value = blockType,
				});
			}

			return new BlocksShape() { ShapeCoords = shapeCoords.ToArray() };
		}

		public void FinishLevel(TowerLevelRunningState finishState)
		{
			if (finishState == TowerLevelRunningState.Running)
				throw new ArgumentException($"Invalid finish state {finishState}");

			LevelData.RunningState = finishState;
			FinishedLevel?.Invoke();
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

			if (!LevelData.IsPlaying)
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
			ValidateAnalogMoveOffset();
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
						var actions = new List<GridAction>() { new ClearMatchedAction() { MatchedType = MatchScoringType.Horizontal, Coords = clearCoords } };
						StartCoroutine(RunActions(actions));
					}
				}
			}
#endif
		}
	}
}