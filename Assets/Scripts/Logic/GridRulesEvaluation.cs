using System.Collections.Generic;
using System.Linq;

namespace TetrisTower.Logic
{
	public struct GridRules
	{
		public int MatchHorizontalLines;
		public int MatchVerticalLines;
		public int MatchDiagonalsLines;
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
			List<GridCoords> matched = new List<GridCoords>();
			BlockType lastMatched;

			if (rules.MatchHorizontalLines > 0) {

				for (int row = 0; row < grid.Rows; ++row) {
					matched.Clear();
					lastMatched = grid[row, 0];

					for (int column = 0; column < grid.Columns; ++column) {
						BlockType currentType = grid[row, column];

						if (currentType == lastMatched && currentType != null) {
							matched.Add(new GridCoords(row, column));

						} else {

							if (matched.Count >= rules.MatchHorizontalLines) {
								var action = new ClearMatchedAction() { Coords = new List<GridCoords>(matched) };
								actions.Add(action);
							}

							matched.Clear();
							if (currentType != null) {
								matched.Add(new GridCoords(row, column));
							}
							lastMatched = currentType;
						}
					}

					if (matched.Count >= rules.MatchHorizontalLines) {
						var action = new ClearMatchedAction() { Coords = matched.ToArray() };
						actions.Add(action);
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
