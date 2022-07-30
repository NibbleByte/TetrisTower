using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;

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
		public event Action FallingShapeSpeedUp;

		private int m_NextRunId = 0;
		private int m_FinishedRuns = 0;

		public bool AreGridActionsRunning => m_FinishedRuns != m_NextRunId;

		public float FallingColumnAnalogOffset { get; private set; } = float.NaN;
		public float FallingShapeAnalogRotateOffset { get; private set; } = float.NaN;
		public float FallingShapeAnalogRotateOffsetBeforeClear { get; private set; } = float.NaN;	// Hacky, I know.

		private float m_FallingSpeedup = 0;

		public void Init(GridLevelData data)
		{
			LevelData = m_DebugLevelData = data;

			if (LevelData.Score == null) {
				LevelData.Score = new ScoreGrid(LevelData.Grid.Rows, LevelData.Grid.Columns, LevelData.Rules);
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

			// In case this instance is reused when loading the same scene.
			StopAllCoroutines();	// Already done on LevelSupervisor unload method.
			m_NextRunId = m_FinishedRuns = 0;
			FallingColumnAnalogOffset = float.NaN;
			FallingShapeAnalogRotateOffset = float.NaN;
			FallingShapeAnalogRotateOffsetBeforeClear = float.NaN;
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

				// This will make sure no matches happen while not playing (won or lost strate).
				// For example: endless game wins on placing any blocks outside, but they could be matching.
				GridRules rules = LevelData.IsPlaying ? LevelData.Rules : new GridRules();
				actions = GameGridEvaluation.Evaluate(Grid, rules);
			}

			// Mark the matching sequence as complete. Used for scoring etc.
			var finishedSequenceActions = new GridAction[] { new MatchingSequenceFinishAction() };
			foreach (var grid in Grids) {
				yield return grid.ApplyActions(finishedSequenceActions);
			}

			m_FinishedRuns++;

			RunningActionsSequenceFinished?.Invoke();

			if (LevelData.IsPlaying) {

				int spawnBlockTypesProgress = LevelData.MatchesToModifySpawnedBlocksRange > 0
					? LevelData.Score.TotalMatched / LevelData.MatchesToModifySpawnedBlocksRange
					: 0
					;

				Vector2Int blocksRange = new Vector2Int();

				switch(LevelData.ModifyBlocksRangeType) {
					case ModifyBlocksRangeType.AddRemoveAlternating:
						int lowerCap = LevelData.BlocksPool.Length - LevelData.InitialSpawnBlockTypesCount - 1;
						blocksRange.x = spawnBlockTypesProgress / 2;

						// Unlikely to happen, but if game goes for too long, start adding blocks in reverse.
						if (blocksRange.x > lowerCap) {
							blocksRange.x = lowerCap - (blocksRange.x - lowerCap);
							blocksRange.x = Mathf.Max(blocksRange.x, 0);
						}

						blocksRange.y = Mathf.Min(LevelData.InitialSpawnBlockTypesCount + (spawnBlockTypesProgress + 1) / 2, LevelData.BlocksPool.Length);
						break;

					case ModifyBlocksRangeType.AddBlocks:
						blocksRange.y = Mathf.Min(LevelData.InitialSpawnBlockTypesCount + spawnBlockTypesProgress, LevelData.BlocksPool.Length);
						break;

					default: throw new NotSupportedException(LevelData.ModifyBlocksRangeType.ToString());
				}

				if (LevelData.ObjectiveRemainingCount == 0 && !LevelData.IsEndlessGame && !AreGridActionsRunning) {
					Debug.Log($"Remaining blocks to clear are 0. Player won.");
					FinishLevel(TowerLevelRunningState.Won);

				} else if (LevelData.SpawnBlockTypesRange != blocksRange) {
					Debug.Log($"Changing blocks range from {LevelData.SpawnBlockTypesRange} to {blocksRange}.", this);
					LevelData.SpawnBlockTypesRange = blocksRange;
					SpawnBlockTypesCountChanged?.Invoke();
				}
			}
		}

		private void SelectFallingShape()
		{
			if (LevelData.NextShape == null) {
				LevelData.NextShape = GenerateShape();
			}

			BlocksShape selectedShape = LevelData.NextShape;
			LevelData.NextShape = GenerateShape();

			SelectFallingShape(selectedShape);
		}


		public void SelectFallingShape(BlocksShape shape)
		{
			if (LevelData.FallingShape != null)
				throw new InvalidOperationException("Trying to select shape while another is already selected");

			LevelData.FallingShape = shape;
			// 0 means shape starts at the top of the grid and continues above.
			// Grid is bigger than playable area, able to accommodate shape placed on top of the top-most flashing blocks.
			// Give 1 block height for the player to react in the worst scenario.
			LevelData.FallDistanceNormalized = shape.Rows - 1.2f;

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

		public void ClearSelectedShape()
		{
			LevelData.FallingShape = null;
			LevelData.FallDistanceNormalized = 0f;

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

			FallingShapeAnalogRotateOffsetBeforeClear = FallingShapeAnalogRotateOffset;
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

			FallingShapeSpeedUp?.Invoke();

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

				// Could finish level inside RunActions.
				if (LevelData.IsPlaying) {
					LevelData.FallSpeedNormalized += LevelData.FallSpeedupPerAction;
					SelectFallingShape();
				}
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
				FinishLevel(LevelData.IsEndlessGame ? TowerLevelRunningState.Won : TowerLevelRunningState.Lost);
			}
		}

		public BlocksShape GenerateShape()
		{
			GridShapeTemplate template = LevelData.ShapeTemplates[LevelData.Random.Next(LevelData.ShapeTemplates.Length)];
			return GenerateShape(template);
		}

		private BlocksShape GenerateShape(GridShapeTemplate template)
		{
			var shapeCoords = new List<BlocksShape.ShapeBind>();
			Vector2Int blocksRange = LevelData.SpawnBlockTypesRange;

			double totalRange = blocksRange.y - blocksRange.x;
			if (LevelData.SpawnWildBlocksChance > 0f) {
				totalRange += LevelData.SpawnWildBlocksChance;
			}


			foreach (GridCoords coords in template.ShapeTemplate) {
				double blockIndex = LevelData.Random.NextDouble() * totalRange;

				BlockType blockType = blockIndex < blocksRange.y
					? LevelData.BlocksPool[blocksRange.x + (int)blockIndex]
					: LevelData.BlocksSet.WildBlock
					;

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

			if (LevelData.RunningState == TowerLevelRunningState.Won) {

				for (int row = 0; row < LevelData.PlayableSize.Row; ++row) {
					for (int column = 0; column < LevelData.Grid.Columns; ++column) {
						if (LevelData.Grid[row, column] == null) {
							LevelData.Score.IncrementWonBonusScore();
						}
					}
				}
			}

			ClearSelectedShape();
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

			if (LevelData.FallingShape != null) {
				UpdateFallShape();

			} else if (!AreGridActionsRunning && LevelData.IsPlaying && CanSelectNextShape()) {
				SelectFallingShape();
			}

			if (LevelData.IsPlaying) {
				LevelData.PlayTime += Time.deltaTime;
			}
		}

		void LateUpdate()
		{
			ValidateAnalogMoveOffset();
		}

		private bool CanSelectNextShape()
		{
			if (LevelData.NextShape == null || LevelData.FallingShape != null)
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
			if (LevelData.FallFrozen && m_FallingSpeedup == 0f)
				return;

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


	}
}