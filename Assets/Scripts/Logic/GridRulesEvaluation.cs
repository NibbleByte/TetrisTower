using DevLocker.GFrame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TetrisTower.Logic
{
	public enum MatchScoringType
	{
		Horizontal = 1 << 0,
		Vertical = 1 << 1,
		Diagonals = 1 << 2,
	}

	[System.Serializable]
	public struct GridRules
	{
		public int MatchHorizontalLines;
		public int MatchVerticalLines;
		public int MatchDiagonalsLines;

		[EnumMask]
		public MatchScoringType ObjectiveType;
		public bool IsObjectiveAllMatchTypes => ObjectiveType == (MatchScoringType.Horizontal | MatchScoringType.Vertical | MatchScoringType.Diagonals);

		// Will wrap around the first and last column when matching and moving.
		public bool WrapSidesOnMatch;
		public bool WrapSidesOnMove;
	}

	public static class GameGridEvaluation
	{
		public static List<GridAction> Evaluate(BlocksGrid grid, GridRules rules)
		{
			List<GridAction> actions = new List<GridAction>();

			EvaluateMatches(grid, rules, actions);

			if (actions.Count > 0)
				return actions;

			EvaluateFalls(grid, rules, actions);

			if (actions.Count > 0)
				return actions;

			return actions;
		}

		private static void EvaluateMatches(BlocksGrid grid, GridRules rules, List<GridAction> actions)
		{
			if (rules.MatchHorizontalLines > 0) {
				EvaluateMatchesAlongLine(new GridCoords(0, 1), grid, rules.MatchHorizontalLines, rules.WrapSidesOnMatch, MatchScoringType.Horizontal, actions);
			}
			if (rules.MatchVerticalLines > 0) {
				EvaluateMatchesAlongLine(new GridCoords(1, 0), grid, rules.MatchVerticalLines, false, MatchScoringType.Vertical, actions);
			}
			if (rules.MatchDiagonalsLines > 0) {
				EvaluateMatchesAlongDiagonal(new GridCoords(-1, -1), grid, rules.MatchDiagonalsLines, rules.WrapSidesOnMatch, actions);
				EvaluateMatchesAlongDiagonal(new GridCoords(+1, -1), grid, rules.MatchDiagonalsLines, rules.WrapSidesOnMatch, actions);
			}
		}

		private static void EvaluateMatchesAlongLine(GridCoords direction, BlocksGrid grid, int matchCount, bool wrapColumns, MatchScoringType matchType, List<GridAction> actions)
		{
			UnityEngine.Debug.Assert(direction.Row == 0 || direction.Column == 0);

			GridCoords coords = new GridCoords(0, 0);
			List<GridCoords> matched = new List<GridCoords>();
			BlockType lastMatched = null;

			bool wrapMatchPending = false;
			List<GridCoords> toBeWrappedMatch = new List<GridCoords>();

			int totalBlocks = grid.Rows * grid.Columns;

			for (int i = 0; i < totalBlocks; ++i) {
				BlockType currentType = grid[coords];

				if (currentType == lastMatched && currentType != null) {
					matched.Add(coords);

				} else {

					if (!wrapMatchPending) {
						if (matched.Count >= matchCount) {
							var action = new ClearMatchedAction() { MatchedType = matchType, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}
					} else {
						// These will be added to the match at the end of the row.
						toBeWrappedMatch.AddRange(matched);
					}
					wrapMatchPending = false;

					matched.Clear();
					if (currentType != null) {
						matched.Add(coords);
					}
					lastMatched = currentType;

					// Don't match sequence if it will be matched by wrapping the end of the row.
					if (wrapColumns && coords.Column == 0 && currentType != null && grid[coords.Row - direction.Row, grid.Columns - 1] == currentType) {
						wrapMatchPending = true;
					}
				}


				coords += direction;


				bool endReached = false;

				if (coords.Row >= grid.Rows) {
					coords.Row = 0;
					coords.Column++;
					endReached = true;
				}
				if (coords.Column >= grid.Columns) {
					coords.Column = 0;
					coords.Row++;
					endReached = true;
				}


				if (endReached) {

					matched.AddRange(toBeWrappedMatch);
					toBeWrappedMatch.Clear();
					wrapMatchPending = false;

					if (matched.Count >= matchCount) {
						var action = new ClearMatchedAction() { MatchedType = matchType, Coords = new List<GridCoords>(matched) };
						actions.Add(action);
					}

					matched.Clear();
					lastMatched = null;
				}
			}
		}

		private static void EvaluateMatchesAlongDiagonal(GridCoords direction, BlocksGrid grid, int matchCount, bool wrapColumns, List<GridAction> actions)
		{
			UnityEngine.Debug.Assert(direction.Row !=0 && direction.Column != 0);

			GridCoords coords;
			List<GridCoords> matched = new List<GridCoords>();
			BlockType lastMatched;

			// Diagonal indexes move along the left and then the bottom edge, i.e. 0th column then 0th row.
			// For each diagonal index, move along the diagonal direction and start matching.
			int totalDiagonals = grid.Rows + grid.Columns - 1;

			// If WrapColumns is true, moving along the diagonal will cover eventually the skipped indexes.
			for (int diagonalIndex = wrapColumns ? grid.Rows - 1 : 0; diagonalIndex < totalDiagonals; ++diagonalIndex) {

				matched.Clear();
				lastMatched = null;

				// Check the Documentation folder for more explanation.
				if (direction.Row == +1 && direction.Column == +1) {
					coords = new GridCoords(Math.Max(0, grid.Rows - diagonalIndex - 1), Math.Max(0, diagonalIndex + 1 - grid.Rows));

				} else if (direction.Row == -1 && direction.Column == -1) {
					coords = new GridCoords(Math.Min(grid.Rows - 1, diagonalIndex), Math.Min(grid.Columns - 1, grid.Columns - 2 - (diagonalIndex - grid.Rows)));

				} else if (direction.Row == +1 && direction.Column == -1) {
					coords = new GridCoords(Math.Max(0, grid.Rows - diagonalIndex - 1), Math.Min(grid.Columns - 1, grid.Columns - 2 - (diagonalIndex - grid.Rows)));

				} else if (direction.Row == -1 && direction.Column == +1) {
					coords = new GridCoords(Math.Min(grid.Rows - 1, diagonalIndex), Math.Max(0, diagonalIndex + 1 - grid.Rows));

				} else {
					throw new ArgumentException("Invalid direction " + direction);
				}

				while (coords.IsInside(grid)) {

					BlockType currentType = grid[coords];

					if (currentType == lastMatched && currentType != null) {
						matched.Add(coords);

					} else {

						if (matched.Count >= matchCount) {
							var action = new ClearMatchedAction() { MatchedType = MatchScoringType.Diagonals, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}

						matched.Clear();
						if (currentType != null) {
							matched.Add(coords);
						}
						lastMatched = currentType;
					}


					coords += direction;


					bool endReached = false;

					if (coords.Row >= grid.Rows || coords.Row < 0) {
						endReached = true;
					}
					if (coords.Column >= grid.Columns || coords.Column < 0) {
						if (wrapColumns) {
							coords.WrapColumn(grid);
						} else {
							endReached = true;
						}
					}


					if (endReached) {
						if (matched.Count >= matchCount) {
							var action = new ClearMatchedAction() { MatchedType = MatchScoringType.Diagonals, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}

						matched.Clear();
						lastMatched = null;
					}
				}
			}
		}

		private static void EvaluateFalls(BlocksGrid grid, GridRules rules, List<GridAction> actions)
		{
			List<KeyValuePair<GridCoords, GridCoords>> falls = new List<KeyValuePair<GridCoords, GridCoords>>();

			for (int column = 0; column < grid.Columns; ++column) {
				int emptyRowsCount = 0;
				falls.Clear();

				for(int row = 0; row < grid.Rows; ++row) {
					BlockType currentType = grid[row, column];

					if (currentType == null) {
						emptyRowsCount++;

					} else if (emptyRowsCount > 0) {

						var start = new GridCoords(row, column);
						var end = new GridCoords(row - emptyRowsCount, column);
						falls.Add(new KeyValuePair<GridCoords, GridCoords>(start, end));
					}
				}

				if (falls.Count > 0) {
					var action = new MoveCellsAction() { MovedCells = falls.ToArray() };
					actions.Add(action);
				}
			}
		}
	}
}
