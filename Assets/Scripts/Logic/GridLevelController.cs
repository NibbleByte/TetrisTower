using DevLocker.GFrame.Timing;
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

		public WiseTiming Timing { get; private set; }

		public List<GameGrid> Grids { get; private set; } = new List<GameGrid>();
		public BlocksGrid Grid => LevelData?.Grid;

		public bool IsPaused { get; private set; } = false;

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

		public void Init(GridLevelData data, WiseTiming timing)
		{
			LevelData = m_DebugLevelData = data;
			Timing = timing;

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

			LevelData.Objectives.RemoveAll(obj => obj == null);

			// Objectives are initialized by the level supervisor after everything is setup.

			// If paused previously, resume (since level may be reused on reload).
			IsPaused = false;

			Timing.PreUpdate += LevelUpdate;
			Timing.PostUpdate += LevelLateUpdate;
		}

		public void StartRunActions(IList<GridAction> actions)
		{
			Timing.StartCoroutine(RunActions(actions), this);
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
				GridRules rules = LevelData.IsPlaying ? LevelData.Rules : new GridRules() {
					MatchHorizontalLines = 0,
					MatchVerticalLines = 0,
					MatchDiagonalsLines = 0,
				};
				actions = GameGridEvaluation.Evaluate(Grid, rules);
			}

			// Mark the matching sequence as complete. Used for scoring etc.
			var finishedSequenceActions = new GridAction[] { new EvaluationSequenceFinishAction() };
			foreach (var grid in Grids) {
				yield return grid.ApplyActions(finishedSequenceActions);
			}

			m_FinishedRuns++;

			RunningActionsSequenceFinished?.Invoke();

			if (LevelData.IsPlaying) {

				int spawnBlockTypesProgress = LevelData.Rules.MatchesToModifySpawnedBlocksRange > 0
					? LevelData.Score.TotalClearedBlocksCount / LevelData.Rules.MatchesToModifySpawnedBlocksRange
					: 0
					;

				Vector2Int blocksRange = new Vector2Int();

				switch(LevelData.Rules.ModifyBlocksRangeType) {
					case ModifyBlocksRangeType.AddRemoveAlternating:
						int lowerCap = LevelData.NormalBlocksSkins.Length - LevelData.InitialSpawnBlockTypesCount - 1;
						blocksRange.x = spawnBlockTypesProgress / 2;

						// Unlikely to happen, but if game goes for too long, start adding blocks in reverse.
						if (blocksRange.x > lowerCap) {
							blocksRange.x = lowerCap - (blocksRange.x - lowerCap);
							blocksRange.x = Mathf.Max(blocksRange.x, 0);
						}

						blocksRange.y = Mathf.Min(LevelData.InitialSpawnBlockTypesCount + (spawnBlockTypesProgress + 1) / 2, LevelData.NormalBlocksSkins.Length);
						break;

					case ModifyBlocksRangeType.AddBlocks:
						blocksRange.y = Mathf.Min(LevelData.InitialSpawnBlockTypesCount + spawnBlockTypesProgress, LevelData.NormalBlocksSkins.Length);
						break;

					default: throw new NotSupportedException(LevelData.Rules.ModifyBlocksRangeType.ToString());
				}


				ObjectiveStatus resolvedStatus = ObjectiveStatus.InProgress;
				if (!AreGridActionsRunning) {
					resolvedStatus = ResolveObjectivesTryFinishLevel();
				}

				// Level finished.
				if (resolvedStatus != ObjectiveStatus.InProgress)
					yield break;

				if (LevelData.SpawnBlockTypesRange != blocksRange) {
					Debug.Log($"Changing blocks range from {LevelData.SpawnBlockTypesRange} to {blocksRange}.", this);
					LevelData.SpawnBlockTypesRange = blocksRange;
					SpawnBlockTypesCountChanged?.Invoke();
				}
			}
		}

		public ObjectiveStatus ResolveObjectivesTryFinishLevel()
		{
			ObjectiveStatus status = ObjectiveStatus.InProgress;

			if (!LevelData.IsPlaying)
				throw new InvalidOperationException();

			foreach (Objective objective in LevelData.Objectives) {

				if (objective.Status == ObjectiveStatus.InProgress)
					continue;

				// Completed status is prioritized over failed.
				if (objective.Status == ObjectiveStatus.Completed) {
					status = ObjectiveStatus.Completed;
					break;
				}

				status = ObjectiveStatus.Failed;
			}

			switch (status) {

				case ObjectiveStatus.Completed:
					FinishLevel(true);
					break;

				case ObjectiveStatus.Failed:
					FinishLevel(false);
					break;
			}

			return status;
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

					if (pair.Coords.Row < Grid.Rows && Grid[pair.Coords] != BlockType.None)
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

						if (pair.Coords.Row < Grid.Rows && Grid[pair.Coords] != BlockType.None) {
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

			if (LevelData.RunningState == TowerLevelRunningState.Preparing) {
				LevelData.RunningState = TowerLevelRunningState.Running;
			}

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

			if (placedInside) {
				yield return RunActions(actions);

				// Could finish level inside RunActions.
				if (LevelData.IsPlaying) {
					LevelData.FallSpeedNormalized += LevelData.FallSpeedupPerAction;
					SelectFallingShape();
				}

			} else {

				Debug.Log($"Placed shape with size ({placedShape.Rows}, {placedShape.Columns}) at {placeCoords}, but that goes outside the play area {LevelData.PlayableSize}, grid ({Grid.Rows}, {Grid.Columns}).", this);

				while (AreGridActionsRunning)
					yield return null;

				m_NextRunId++;

				// Manually place the shape as we don't want to evaluate and execute normal game-play logic. Just stay visually there.
				foreach (var grid in Grids) {
					yield return grid.ApplyActions(actions);
				}

				m_FinishedRuns++;

				PlacedOutsideGrid?.Invoke();

				ObjectiveStatus resolvedStatus = ResolveObjectivesTryFinishLevel();

				// Continue playing if objectives didn't finish the game?
				if (resolvedStatus == ObjectiveStatus.InProgress && LevelData.IsPlaying) {
					Debug.LogWarning("Placing blocks outside did not finish level.", this);
				}
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
			Vector2Int blocksRangeVec = LevelData.SpawnBlockTypesRange;
			float blocksRange = blocksRangeVec.y - blocksRangeVec.x;

			double totalRange = blocksRange;

			if (LevelData.Rules.WildBlockChance > 0f) {
				totalRange += LevelData.Rules.WildBlockChance;
			}
			if (LevelData.Rules.BlockSmiteChance > 0f) {
				totalRange += LevelData.Rules.BlockSmiteChance;
			}
			if (LevelData.Rules.RowSmiteChance > 0f) {
				totalRange += LevelData.Rules.RowSmiteChance;
			}


			foreach (GridCoords coords in template.ShapeTemplate) {
				double blockIndex = LevelData.Random.NextDouble() * totalRange;

				BlockType blockType;
				if (blockIndex < blocksRange) {
					blockType = BlockType.B1 + blocksRangeVec.x + (int)blockIndex;

				} else {
					blockIndex -= blocksRange;

					if (blockIndex < LevelData.Rules.WildBlockChance) {
						blockType = BlockType.SpecialWildBlock;

					} else if (blockIndex < LevelData.Rules.WildBlockChance + LevelData.Rules.BlockSmiteChance) {
						blockType = BlockType.SpecialBlockSmite;

					} else if (blockIndex < LevelData.Rules.WildBlockChance + LevelData.Rules.BlockSmiteChance + LevelData.Rules.RowSmiteChance) {
						blockType = BlockType.SpecialRowSmite;

					} else {
						Debug.LogError($"Generating block produced index {blockIndex + blocksRange} larger than anything allowed.", this);
						blockType = BlockType.WonBonusBlock;
					}
				}

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = coords,
					Value = blockType,
				});
			}

			return new BlocksShape() { ShapeCoords = shapeCoords.ToArray() };
		}

		public void FinishLevel(bool win)
		{
			LevelData.RunningState = TowerLevelRunningState.Finished;
			LevelData.HasWon = win;

			if (LevelData.HasWon) {

				for (int row = 0; row < LevelData.PlayableSize.Row; ++row) {
					for (int column = 0; column < LevelData.Grid.Columns; ++column) {
						if (LevelData.Grid[row, column] == BlockType.None) {
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
			IsPaused = true;
		}

		public void ResumeLevel()
		{
			IsPaused = false;
		}

		private void LevelUpdate()
		{
			if (LevelData.FallingShape != null) {
				UpdateFallShape();

			} else if (!AreGridActionsRunning && LevelData.IsPlaying && CanSelectNextShape()) {
				SelectFallingShape();
			}

			if (LevelData.IsPlaying) {

				if (LevelData.RunningState == TowerLevelRunningState.Preparing) {
					LevelData.PrepareTime += WiseTiming.DeltaTime;
				}

				LevelData.PlayTime += WiseTiming.DeltaTime;

			}
		}

		private void LevelLateUpdate()
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

					if (Grid[coords] != BlockType.None) {
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

			// Don't fall on it's own while player is preparing - waiting for them to start the level.
			if (LevelData.RunningState == TowerLevelRunningState.Preparing)
				return;

			// Clamp to avoid skipping over a cell on hiccups.
			LevelData.FallDistanceNormalized += Mathf.Clamp01(WiseTiming.DeltaTime * (LevelData.FallSpeedNormalized + m_FallingSpeedup));

			var fallCoords = LevelData.FallShapeCoords;

			foreach (var pair in LevelData.FallingShape.AddToCoordsWrapped(fallCoords, Grid)) {

				if (pair.Coords.Row >= Grid.Rows)
					continue;

				if (pair.Coords.Row < 0 || Grid[pair.Coords] != BlockType.None) {

					PlacingFallingShape?.Invoke();

					// The current coords are taken so move back one tile up where it should be free.
					fallCoords.Row++;

					m_FallingSpeedup = 0f;

					var fallShape = LevelData.FallingShape;
					LevelData.FallingShape = null;

					Timing.StartCoroutine(PlaceFallingShape(fallCoords, fallShape), this);
					break;
				}
			}
		}


	}
}