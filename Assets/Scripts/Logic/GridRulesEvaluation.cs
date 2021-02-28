using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;

namespace TetrisTower.Logic
{
	[System.Serializable]
	public struct GridRules
	{
		public int MatchHorizontalLines;
		public int MatchVerticalLines;
		public int MatchDiagonalsLines;

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
				EvaluateMatchesAlong(new GridCoords(0, 1), grid, rules.MatchHorizontalLines, rules.WrapSidesOnMatch, actions);
			}
			if (rules.MatchVerticalLines > 0) {
				EvaluateMatchesAlong(new GridCoords(1, 0), grid, rules.MatchVerticalLines, false, actions);
			}
		}

		private static void EvaluateMatchesAlong(GridCoords direction, BlocksGrid grid, int matchCount, bool wrapColumns, List<GridAction> actions)
		{
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
							var action = new ClearMatchedAction() { Coords = new List<GridCoords>(matched) };
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
					if (wrapColumns && coords.Column == 0 && currentType != null && grid[coords.Row, grid.Columns - 1] == currentType) {
						wrapMatchPending = true;
					}
				}


				coords += direction;


				bool matchInterrupted = false;

				if (coords.Row >= grid.Rows) {
					coords.Row = 0;
					coords.Column++;
					matchInterrupted = true;
				}
				if (coords.Column >= grid.Columns) {
					coords.Column = 0;
					coords.Row++;
					matchInterrupted = true;
				}


				if (matchInterrupted) {

					matched.AddRange(toBeWrappedMatch);
					toBeWrappedMatch.Clear();
					wrapMatchPending = false;

					if (matched.Count >= matchCount) {
						var action = new ClearMatchedAction() { Coords = new List<GridCoords>(matched) };
						actions.Add(action);
					}

					matched.Clear();
					lastMatched = null;
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
