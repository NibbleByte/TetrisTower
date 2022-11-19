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
		public bool IsObjectiveAllMatchTypes =>
			ObjectiveType == (MatchScoringType)~0 ||
			ObjectiveType == (MatchScoringType.Horizontal | MatchScoringType.Vertical | MatchScoringType.Diagonals)
			;

		// Will wrap around the first and last column when matching and moving.
		public bool WrapSidesOnMatch;
		public bool WrapSidesOnMove;
	}

	public static class GameGridEvaluation
	{
		public static List<GridAction> Evaluate(BlocksGrid grid, GridRules rules)
		{
			List<GridAction> actions = new List<GridAction>();

			if (!grid.MatchingFrozen) {
				EvaluateMatches(grid, rules, actions);

				EvaluateSpecialMatches(grid, rules, actions);
			}

			if (actions.Count > 0)
				return actions;

			EvaluateFalls(grid, rules, actions);

			if (actions.Count > 0)
				return actions;

			return actions;
		}

		public static bool IsNormalBlock(this BlockType block)
		{
			return block != BlockType.None && block < BlockType.SpecialWildBlock;
		}

		public static bool IsNormalMatch(this BlockType block)
		{
			return block != BlockType.None && block <= BlockType.SpecialWildBlock;
		}

		public static bool IsStaticBlock(this BlockType block)
		{
			return block >= BlockType.StaticBlock;
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

		private static bool BlocksMatchSafe(BlockType block1, BlockType block2)
		{
			if (block1 == BlockType.None || block2 == BlockType.None)
				return false;

			if (!block1.IsNormalMatch() || !block2.IsNormalMatch())
				return false;

			BlockType type = block1;
			if (block2 > type) {
				type = block2;
			}

			if (type.IsNormalBlock())
				return block1 == block2;

			if (type == BlockType.SpecialWildBlock)
				return true;

			throw new NotSupportedException(block2.ToString());
		}

		private static void BacktrackOrClearMatched(BlocksGrid grid, List<GridCoords> matched)
		{
			// Instead of clearing the matched list, back track all the wild blocks so we can resume with them.
			int removeToIndex;
			for(removeToIndex = matched.Count - 1; removeToIndex >= 0; removeToIndex--) {
				BlockType block = grid[matched[removeToIndex]];

				if (block != BlockType.SpecialWildBlock) {
					break;
				}
			}

			matched.RemoveRange(0, removeToIndex + 1);
		}

		private static void EvaluateMatchesAlongLine(GridCoords direction, BlocksGrid grid, int matchCount, bool wrapColumns, MatchScoringType matchType, List<GridAction> actions)
		{
			UnityEngine.Debug.Assert(direction.Row == 0 || direction.Column == 0);

			GridCoords coords = new GridCoords(0, 0);
			List<GridCoords> matched = new List<GridCoords>();
			BlockType lastMatched = BlockType.None;

			bool wrapMatchPending = false;
			List<GridCoords> toBeWrappedMatch = new List<GridCoords>();

			int totalBlocks = grid.Rows * grid.Columns;

			for (int i = 0; i < totalBlocks; ++i) {
				BlockType currentType = grid[coords];

				if (!currentType.IsNormalMatch()) {
					currentType = BlockType.None;
				}

				if (BlocksMatchSafe(lastMatched, currentType)) {
					matched.Add(coords);

					// If match starts with wild block, it wouldn't know it's actual match type we're looking for. First non-wild block found selects the type.
					if (lastMatched== BlockType.SpecialWildBlock) {
						lastMatched = currentType;
					}

				} else {

					if (!wrapMatchPending) {
						// Exclude matches with only wild blocks.
						if (matched.Count >= matchCount && lastMatched != BlockType.None && lastMatched != BlockType.SpecialWildBlock) {
							var action = new ClearMatchedAction() { MatchedType = matchType, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}
					} else {
						// These will be added to the match at the end of the row.
						toBeWrappedMatch.AddRange(matched);
					}
					wrapMatchPending = false;

					if (currentType != BlockType.None) {
						BacktrackOrClearMatched(grid, matched);
					} else {
						matched.Clear();
					}

					if (currentType != BlockType.None) {
						matched.Add(coords);
					}
					lastMatched = currentType;

					// Don't match sequence if it will be matched by wrapping the end of the row.
					if (wrapColumns && coords.Column == 0 && BlocksMatchSafe(grid[coords.Row - direction.Row, grid.Columns - 1], currentType)) {
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

					if (toBeWrappedMatch.Count > 0) {
						// Find out what the wrapped match block type is. If all matched blocks are wild ones, no wrapped type is available.
						BlockType wrappedType = toBeWrappedMatch.Select(c => grid[c]).FirstOrDefault(b => b.IsNormalBlock());

						if (wrappedType == BlockType.None || wrappedType == lastMatched || lastMatched == BlockType.SpecialWildBlock) {
							// wrappedType == BlockValue.None					=> Everything on the right is wild blocks.
							// lastMatched == MatchType.SpecialWildBlock		=> Everything on the left is wild blocks.
							// wrappedType == lastMatched						=> Both sides have the same type, everything matches.
							matched.AddRange(toBeWrappedMatch);

							// In case of wild blocks on the left, still needs a proper type to be considered a bit later.
							if (wrappedType != BlockType.None) {
								lastMatched = wrappedType;
							}

						} else {

							int mbi; // Match Breaking Index. On the right of it should be cleared together with the wrapped ones. The rest on the left are match on their own.
							for (mbi = matched.Count - 1; mbi >= 0; --mbi) {
								if (!BlocksMatchSafe(grid[matched[mbi]], wrappedType))
									break;
							}

							int tbmbi; // To Be Matched Index. Same but with the other collection.
							for (tbmbi = 0; tbmbi < toBeWrappedMatch.Count; ++tbmbi) {
								if (!BlocksMatchSafe(grid[toBeWrappedMatch[tbmbi]], lastMatched))
									break;
							}

							// -1 means all is matching.
							//	  12   0
							// B W B | W W B	<-- toBeWrappedMatch
							// B W W | W W B
							// C W W | W W B
							// C C W | W W B
							// C C B | W W B
							toBeWrappedMatch.InsertRange(0, matched.GetRange(mbi + 1, matched.Count - (mbi + 1)));

							if (toBeWrappedMatch.Count >= matchCount) {
								var action = new ClearMatchedAction() { MatchedType = matchType, Coords = new List<GridCoords>(toBeWrappedMatch) };
								actions.Add(action);
							}

							// tbmbi can't be toBeWrappedMatch.Count as match types are different (check above) and at least 1 element on the right is not matchable.
							matched.AddRange(toBeWrappedMatch.GetRange(0, tbmbi));
						}
					}


					toBeWrappedMatch.Clear();
					wrapMatchPending = false;

					// lastMatched can be null if only wild blocks present. Exclude matches with only wild blocks.
					if (matched.Count >= matchCount && lastMatched != BlockType.None && lastMatched != BlockType.SpecialWildBlock) {
						var action = new ClearMatchedAction() { MatchedType = matchType, Coords = new List<GridCoords>(matched) };
						actions.Add(action);
					}

					matched.Clear();
					lastMatched = BlockType.None;
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
				lastMatched = BlockType.None;

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

					if (!currentType.IsNormalMatch()) {
						currentType = BlockType.None;
					}

					if (BlocksMatchSafe(lastMatched, currentType)) {
						matched.Add(coords);

						// If match starts with wild block, it wouldn't know it's actual match type we're looking for. First non-wild block found selects the type.
						if (lastMatched == BlockType.SpecialWildBlock) {
							lastMatched = currentType;
						}

					} else {

						// Exclude matches with only wild blocks.
						if (matched.Count >= matchCount && lastMatched != BlockType.None && lastMatched != BlockType.SpecialWildBlock) {
							var action = new ClearMatchedAction() { MatchedType = MatchScoringType.Diagonals, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}

						if (currentType != BlockType.None) {
							BacktrackOrClearMatched(grid, matched);
						} else {
							matched.Clear();
						}

						if (currentType != BlockType.None) {
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
						// lastMatched can be null if only wild blocks present. Exclude matches with only wild blocks.
						if (matched.Count >= matchCount && lastMatched != BlockType.None && lastMatched != BlockType.SpecialWildBlock) {
							var action = new ClearMatchedAction() { MatchedType = MatchScoringType.Diagonals, Coords = new List<GridCoords>(matched) };
							actions.Add(action);
						}

						matched.Clear();
						lastMatched = BlockType.None;
					}
				}
			}
		}

		private static void EvaluateSpecialMatches(BlocksGrid grid, GridRules rules, List<GridAction> actions)
		{
			GridCoords coord;
			ClearMatchedAction action;

			for (int row = 0; row < grid.Rows; ++row) {
				for(int column = 0; column < grid.Columns; ++column) {
					BlockType block = grid[row, column];

					if (block == BlockType.None)
						continue;

					switch(block) {

						case BlockType.SpecialBlockSmite:

							if (row == 0)
								continue;

							BlockType matchedType = grid[row - 1, column];
							if (!matchedType.IsNormalBlock())
								continue;

							// Gather all blocks of the type below this one. Include this one too.
							List<GridCoords> matched = new List<GridCoords>();
							matched.Add(new GridCoords(row, column));

							// Resume matching from this position. Looks better visually.
							coord = new GridCoords(row - 1, column);
							while(coord.Row != row || coord.Column != column) {
								if (grid[coord] == matchedType) {
									matched.Add(coord);
								}

								coord.Row--;
								if (coord.Row < 0) {
									coord.Row = grid.Rows - 1;
									coord.Column = (coord.Column + 1) % grid.Columns;
								}
							}

							// One match is always guaranteed. Add this block as well.
							action = new ClearMatchedAction() {
								MatchedType = 0,
								SpecialMatch = true,
								Coords = matched
							};
							actions.Add(action);
							break;


						case BlockType.SpecialRowSmite:

							action = new ClearMatchedAction() {
								MatchedType = 0,
								SpecialMatch = true,
								Coords = Enumerable
									.Range(0, grid.Columns)
									// Resume matching from this position. Looks better visually.
									.Select(c => new GridCoords(row, (c + column) % grid.Columns))
									.Where(c => grid[c] != BlockType.None && !grid[c].IsStaticBlock())
									.ToList()	// Including me
							};
							actions.Add(action);
							break;
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

					if (currentType != BlockType.None && grid.IsPinnedCoords(new GridCoords(row, column))) {
						emptyRowsCount = 0;
						continue;
					}

					if (currentType == BlockType.None) {
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
