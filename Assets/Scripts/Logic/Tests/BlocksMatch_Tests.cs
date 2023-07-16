using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using NUnit.Framework;
using TetrisTower.Game;
using TetrisTower.Logic.Objectives;
using UnityEngine;
using UnityEngine.TestTools;

namespace TetrisTower.Logic
{
	// Test matching logic corner cases.
	// Tests create a grid, place some blocks and run grid actions.
	// Checks if grid actions procude the expected grid state.
	// https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/edit-mode-vs-play-mode-tests.html
	public class BlocksMatch_Tests
	{
		// For copy-paste of empty line.
		// { N, N, N, N, N, N, N, N, N, N, N, N, N },

		private BlockType R = BlockType.B1; // "Red" block
		private BlockType G = BlockType.B2; // "Green" block
		private BlockType B = BlockType.B3; // "Blue" block

		private BlockType W = BlockType.SpecialWildBlock;	// "Wild" block
		private BlockType Q = BlockType.SpecialBlockSmite;	// "Block Smite"
		private BlockType H = BlockType.SpecialRowSmite;	// "Row Smite"

		private BlockType S = BlockType.StaticBlock;		// "Static" block

		private BlockType N = BlockType.None;				// "null" block

		private const int MaxRows = 13;
		private const int MaxColumns = 13;

		private static GridRules DefaultRules => new GridRules {
			MatchHorizontalLines = 3,
			MatchVerticalLines = 3,
			MatchDiagonalsLines = 3,

			WrapSidesOnMatch = true,
			WrapSidesOnMove = true,
		};

		private BlocksGrid m_Grid;
		private ScoreGrid m_ScoreGrid;
		private MatchBlocksCount_Objective m_MatchObjective;
		private GameGrid[] m_Grids;

		private bool m_SkipGridSetup = false;

		private readonly GridAction[] m_FinishActions = new EvaluationSequenceFinishAction[] { new EvaluationSequenceFinishAction() };


		#region Setup

		[OneTimeSetUp]
		//public void OneTimeSetUp()
		//{
		//	Debug.Log("OneTimeSetup");
		//}

		// This is called before every test in this class
		[SetUp]
		public void Setup()
		{
			// HACK: Keep this method as Unity tries to execute the other methods named Setup*.
			//Debug.Log("Setup");
			m_SkipGridSetup = false;
		}

		private PlaceAction SetupPlaceAction(BlockType[,] blocks)
		{
			var placeCoords = new List<GridShape<BlockType>.ShapeBind>();

			for (int row = blocks.GetLength(0) - 1; row >= 0; --row) {
				for (int column = 0; column < blocks.GetLength(1); ++column) {
					var coords = new GridCoords(blocks.GetLength(0) - 1 - row, column);
					placeCoords.Add(BlocksShape.Bind(coords, blocks[row, column]));
				}
			}

			return new PlaceAction() {
				PlaceCoords = GridCoords.Zero,
				PlacedShape = new BlocksShape() { ShapeCoords = placeCoords.ToArray() }
			};
		}

		private IEnumerator SetupGrid(BlockType[,] blocks, GridRules rules, MatchScoringType objectiveMatchTypes, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
		{
			Assert.LessOrEqual(blocks.GetLength(0), MaxRows, $"Wrong rows count.\n{caller}:{lineNumber}");
			Assert.LessOrEqual(blocks.GetLength(1), MaxColumns, $"Wrong columns count.\n{caller}:{lineNumber}");

			m_Grid = new BlocksGrid(MaxRows, MaxColumns);
			m_ScoreGrid = new ScoreGrid(MaxRows, MaxColumns, rules);
			m_MatchObjective = new MatchBlocksCount_Objective();
			m_MatchObjective.Init(rules, m_ScoreGrid);
			m_MatchObjective.MatchesEndCount = int.MaxValue;
			m_MatchObjective.MatchesType = objectiveMatchTypes;

			m_Grids = new GameGrid[] { m_Grid, m_ScoreGrid };

			yield return m_Grid.ApplyActions(new GridAction[] { SetupPlaceAction(blocks) });
		}

		[TearDown]
		public void TearDown()
		{
			m_Grid = null;
			m_ScoreGrid = null;
			m_MatchObjective = null;
			m_Grids = null;
			m_SkipGridSetup = false;
		}

		#endregion

		#region Utils

		private void PrintGrid()
		{
			string GetBlockSymbol(BlockType block)
			{
				if (block == R) return nameof(R);
				if (block == G) return nameof(G);
				if (block == B) return nameof(B);

				if (block == W) return nameof(W);
				if (block == Q) return nameof(Q);
				if (block == H) return nameof(H);
				if (block == S) return nameof(S);

				if (block == N) return nameof(N);

				throw new System.NotSupportedException(block.ToString());
			}


			var output = new StringBuilder();

			output.AppendLine("Grid: ");

			for (int row = m_Grid.Rows - 1; row >= 0; --row) {

				output.Append($"{row}  [ ");

				for (int column = 0; column < m_Grid.Columns; ++column) {
					output.Append(GetBlockSymbol(m_Grid[row, column]));

					if (column != m_Grid.Columns - 1) {
						output.Append(", ");
					}
				}

				output.AppendLine(" ]");
			}

			// Header columns
			//output.Append("      ");
			//for (int column = 0; column < m_Grid.Columns; ++column) {
			//	output.Append($"{column}\t");
			//}
			//
			//output.AppendLine();

			output.AppendLine("       0  1   2   3  4  5   6   7   8  9  10 11 12");	// Tabs are too wide.

			Debug.Log(output.ToString());
		}

		private void AssertGrid(BlockType[,] blocks, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
		{
			for (int row = 0; row < MaxRows; ++row) {

				for (int column = 0; column < blocks.GetLength(1); ++column) {
					int blocksRow = blocks.GetLength(0) - 1 - row;

					BlockType block = blocksRow >= 0 ? blocks[blocksRow, column] : BlockType.None;
					BlockType gridBlock = m_Grid[row, column];

					if (block != gridBlock) {
						PrintGrid();
						System.Diagnostics.Debugger.Break();
					}

					Assert.AreEqual(block, m_Grid[row, column], $"Blocks[{blocksRow}, {column}] != Grid[{row}, {column}]\n{caller}:{lineNumber}");
				}
			}
		}

		private void AssertNoActions(GridRules rules, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
		{
			List<GridAction> pendingActions = GameGridEvaluation.Evaluate(m_Grid, rules);

			if (pendingActions.Count > 0) {
				PrintGrid();
				System.Diagnostics.Debugger.Break();
			}

			Assert.AreEqual(0, pendingActions.Count, $"Grid Actions left: {pendingActions.Count}.\n{caller}:{lineNumber}");
		}

		private static BlockType[,] Mirror(BlockType[,] blocks)
		{
			BlockType[,] mirror = new BlockType[blocks.GetLength(0), blocks.GetLength(1)];

			for(int row = 0; row < blocks.GetLength(0); ++row) {
				for(int column = 0; column < blocks.GetLength(1); ++column) {
					mirror[row, blocks.GetLength(1) - 1 - column] = blocks[row, column];
				}
			}

			return mirror;
		}

		private IEnumerator EvaluateGrid(BlockType[,] blocks, BlockType[,] blocksDone, int applyActionsCount, int resultScore = -1, bool doMirror = true, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
		{
			yield return EvaluateGrid(blocks, blocksDone, DefaultRules, (MatchScoringType)~0, applyActionsCount, resultScore, doMirror, lineNumber, caller);
		}

		private IEnumerator EvaluateGrid(BlockType[,] blocks, BlockType[,] blocksDone, GridRules rules, MatchScoringType objectiveMatchTypes, int applyActionsCount, int resultScore = -1, bool doMirror = true, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
		{
			if (!m_SkipGridSetup) {
				yield return SetupGrid(blocks, rules, objectiveMatchTypes, lineNumber, caller);
			} else {
				Assert.IsFalse(doMirror, $"Can't do mirror if using custom grid setup.\n{caller}:{lineNumber}");
			}

			for(int i = 0; i < applyActionsCount; ++i) {
				var actions = GameGridEvaluation.Evaluate(m_Grid, rules);
				Assert.AreNotEqual(0, actions.Count, $"No actions available.\n{caller}:{lineNumber}");

				foreach(GameGrid grid in m_Grids) {
					yield return grid.ApplyActions(actions);
				}
			}

			yield return m_ScoreGrid.ApplyActions(m_FinishActions);

			AssertNoActions(rules, lineNumber, caller);
			AssertGrid(blocksDone, lineNumber, caller);

			if (resultScore >= 0) {
				Assert.AreEqual(resultScore, m_ScoreGrid.Score, $"Score is wrong.\n{caller}:{lineNumber}");
			}

			//
			// Mirrored
			//
			if (doMirror) {

				yield return SetupGrid(Mirror(blocks), rules, objectiveMatchTypes, lineNumber, caller);

				for (int i = 0; i < applyActionsCount; ++i) {
					var actions = GameGridEvaluation.Evaluate(m_Grid, rules);

					foreach (GameGrid grid in m_Grids) {
						yield return grid.ApplyActions(actions);
					}
				}

				yield return m_ScoreGrid.ApplyActions(m_FinishActions);

				AssertNoActions(rules, lineNumber, caller);
				AssertGrid(Mirror(blocksDone), lineNumber, caller);

				if (resultScore >= 0) {
					Assert.AreEqual(resultScore, m_ScoreGrid.Score, $"Score is wrong.\n{caller}:{lineNumber}");
				}

			}
		}

		#endregion


		#region Grid Setup Tests

		[UnityTest]
		public IEnumerator GridSetup()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
			};

			BlockType[,] blocks2 = new BlockType[,] {
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
			};

			BlockType[,] blocks9 = new BlockType[,] {
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
			};

			yield return SetupGrid(blocks, DefaultRules, (MatchScoringType)~0);

			Assert.AreEqual(MaxRows, m_Grid.Rows);
			Assert.AreEqual(MaxColumns, m_Grid.Columns);

			AssertGrid(blocks);

			yield return SetupGrid(blocks2, DefaultRules, (MatchScoringType)~0);

			AssertGrid(blocks2);

			yield return SetupGrid(blocks9, DefaultRules, (MatchScoringType)~0);

			AssertGrid(blocks9);
		}

		#endregion

		#region Basics Tests

		[UnityTest]
		public IEnumerator Basics_Horizontal()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, R, N, N, N, N, N, N, N, B },
				{ R, R, R, B, B, B, N, S, S, S, N, N, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, R, N, N, S, S, S, N, N, B },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, 3*2 + 3);
		}

		[UnityTest]
		public IEnumerator Basics_Vertical()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, B, N, N, N, N, N, N, N, S, N },
				{ R, N, B, B, N, R, N, N, N, S, N, S, N },
				{ R, N, B, B, N, R, N, B, N, S, N, S, N },
				{ R, R, R, B, N, R, N, B, N, S, N, S, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, S, N },
				{ N, N, N, N, N, N, N, N, N, S, N, S, N },
				{ N, N, B, N, N, N, N, B, N, S, N, S, N },
				{ N, N, B, N, N, N, N, B, N, S, N, S, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, 3*2 + 3*2 + 3);
		}

		[UnityTest]
		public IEnumerator Basics_Diagonals()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, R, N },
				{ G, B, N, N, N, N, N, N, N, N, R, N, N },
				{ B, R, B, S, N, N, N, B, N, R, N, N, G },
				{ R, B, S, S, N, R, B, N, G, N, N, G, B },
				{ B, S, S, S, R, B, N, G, N, N, N, B, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, S, N, N, N, N, N, N, N, N, N },
				{ N, N, S, S, N, N, N, N, N, N, N, N, N },
				{ N, S, S, S, R, R, N, G, G, N, N, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, 3 + 3 + 3*2 + 3 + 3 + 3);
		}

		[UnityTest]
		public IEnumerator Basics_MaxMatch()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, R, N, B, N, N, N, B, N, N, R, N },
				{ N, R, B, N, G, B, N, B, G, N, N, B, R },
				{ W, B, B, N, G, G, W, G, G, N, N, B, B },
				{ W, R, R, N, B, B, W, B, B, N, N, R, R },
				{ W, R, R, N, B, B, W, B, B, N, N, R, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 1, (3 * 17) * 2);
		}

		[UnityTest]
		public IEnumerator Basics_WholeRow()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ R, R, R, R, R, R, R, R, R, R, R, R, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 1, 3*11);
		}

		[UnityTest]
		public IEnumerator Basics_WholeColumn()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
				{ R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 1, 3*11);
		}

		#endregion

		#region Wild Block Tests

		[UnityTest]
		public IEnumerator WildBlock_Basics_Horizontal()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ R, R, R, W, B, B, N, N, N, N, N, N, N },
				{ R, R, R, W, B, N, N, N, N, N, N, N, N },
				{ S, S, S, N, S, S, S, N, N, N, N, N, N },
				{ R, R, W, B, B, N, N, N, W, N, N, N, N },
				{ R, R, W, B, N, N, N, N, S, S, N, N, N },
				{ S, S, S, N, S, S, S, N, W, W, N, N, N },
				{ R, W, R, N, B, W, B, N, S, S, S, N, N },
				{ S, S, S, N, S, S, S, N, W, W, W, N, N },
				{ R, W, W, N, W, W, B, N, S, S, S, S, N },
				{ R, R, W, N, W, B, B, N, W, W, W, W, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, W, N, N, N, N },
				{ N, N, N, N, N, N, N, N, S, S, N, N, N },
				{ N, N, N, N, N, N, N, N, W, W, N, N, N },
				{ N, N, N, N, B, N, N, N, S, S, S, N, N },
				{ S, S, S, N, S, S, S, N, W, W, W, N, N },
				{ S, S, S, N, S, S, S, N, S, S, S, S, N },
				{ S, S, S, B, S, S, S, N, W, W, W, W, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, (3 + 3) + (3 + 3) + (3 + 3) + 3 + 3*2 + 3*2 + 3*3);
		}

		[UnityTest]
		public IEnumerator WildBlock_Basics_Vertical()
		{
			BlockType[,] blocks_horizontal = new BlockType[,] {
				{ R, R, R, W, B, B, N, N, N, N, N, N, N },
				{ R, R, R, W, B, N, N, N, N, N, N, N, N },
				{ S, S, S, N, S, S, S, N, N, N, N, N, N },
				{ R, R, W, B, B, N, N, N, W, N, N, N, N },
				{ R, R, W, B, N, N, N, N, S, S, N, N, N },
				{ S, S, S, N, S, S, S, N, W, W, N, N, N },
				{ R, W, R, N, B, W, B, N, S, S, S, N, N },
				{ S, S, S, N, S, S, S, N, W, W, W, N, N },
				{ R, W, W, N, W, W, B, N, S, S, S, S, N },
				{ R, R, W, N, W, B, B, N, W, W, W, W, N },
			};

			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ W, S, N, N, N, N, N, N, N, N, N, N, N },
				{ W, S, W, S, N, N, N, N, N, N, N, N, N },
				{ W, S, W, S, W, S, N, N, N, N, N, N, N },
				{ W, S, W, S, W, S, W, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ B, B, S, W, S, N, N, S, N, N, N, N, N },
				{ B, W, S, B, S, N, N, S, N, B, N, N, N },
				{ W, W, S, W, S, N, B, S, B, B, N, N, N },
				{ N, N, N, N, N, B, B, N, W, W, N, N, N },
				{ W, W, S, R, S, W, W, S, R, R, N, N, N },
				{ R, W, S, W, S, R, R, S, R, R, N, N, N },
				{ R, R, S, R, S, R, R, S, R, R, N, N, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, W, N, N, N, N, N, N, N, N, N, N },
				{ N, N, W, N, W, N, N, N, N, N, N, N, N },
				{ N, N, W, N, W, N, N, N, N, N, N, N, N },
				{ N, N, S, N, S, N, N, S, N, N, N, N, N },
				{ N, N, S, N, S, N, N, S, N, N, N, N, N },
				{ W, S, S, N, S, N, N, S, N, N, N, N, N },
				{ W, S, S, S, S, S, N, S, N, N, N, N, N },
				{ W, S, S, S, S, S, N, S, N, N, N, N, N },
				{ W, S, S, S, S, B, W, S, B, N, N, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, (3 + 3) + (3 + 3) + (3 + 3) + 3 + 3 * 2 + 3 * 2 + 3 * 3);
		}

		[UnityTest]
		public IEnumerator WildBlock_Complex()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ W, R, W, R, W, R, W, R, W, R, W, R, W },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ W, N, B, N, N, N, N, N, N, N, N, N, N },
				{ R, N, B, N, N, N, N, N, N, N, N, N, N },
				{ R, R, W, B, B, W, S, W, W, B, N, N, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, S, N, N, N, N, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3*3 + 3 + 3*11);
		}

		[UnityTest]
		public IEnumerator WildBlock_Horizontal_Backtrack()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, R, W, B, B, N, R, W, N, B, B, N, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, R, N, N, N, N, R, W, N, B, B, N, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 1, 3);
		}

		[UnityTest]
		public IEnumerator WildBlock_Horizontal_Backtrack_Wrapped()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ B, B, N, N, N, N, N, N, N, N, R, W, N },
					{ B, N, N, N, N, N, N, N, N, N, R, W, B },
					{ S, S, S, N, N, N, N, N, N, N, S, S, S },
					{ N, B, B, N, N, N, N, N, N, N, N, R, W },
					{ B, B, N, N, N, N, N, N, N, N, N, R, W },
				};
				/*
				BlockValue[,] blocks = new BlockValue[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
					{ N, B, N, N, N, N, N, N, N, N, R, S, N },
					{ B, S, S, N, N, N, N, N, N, N, R, R, S },
					{ S, B, B, N, N, N, N, N, N, N, S, R, W }, // W matches by the 2 diagonals.
				};
				*/

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
					{ N, S, S, N, N, N, N, N, N, N, R, S, N },
					{ S, B, B, N, N, N, N, N, N, N, S, R, S },
				};

				yield return EvaluateGrid(blocks, blocksDone, 4, 3 + 3 + (3 + 3) * 2);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, B, N, N, N, N, N, N, N, N, R, W },
					{ W, B, N, N, N, N, N, N, N, N, N, R, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3*3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, N, N, N, N, N, N, N, N, R, R, W },
					{ W, B, N, N, N, N, N, N, N, N, W, R, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*3 + 3*3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, N, N, N, N, N, N, N, N, N, N, W },
					{ W, B, N, N, N, N, N, N, N, N, N, W, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ B, B, N, N, N, N, N, N, N, N, N, N, W },
					{ B, B, N, N, N, N, N, N, N, N, N, W, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, N, N, N, N, N, N, N, N, N, N, N, B },
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ W, N, N, N, N, N, N, N, N, N, N, N, B },
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				yield return EvaluateGrid(blocks, blocksDone, 0, 0);
			}
		}


		[UnityTest]
		public IEnumerator WildBlock_Basics_Diagonals()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, W, B, N },
					{ N, N, W, B, N, N, W, B, N, R, W, N, N },
					{ N, W, W, N, N, R, B, N, R, W, N, N, N },
					{ R, W, N, N, R, W, N, R, W, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, (3 + 3) + (3 + 3) + (3 * 2 + 3 * 2));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, B, N, N, N },
					{ N, N, N, B, B, N, N, B, B, N, N, N, N },
					{ N, N, W, B, N, N, W, W, N, N, N, N, N },
					{ N, W, W, N, N, R, R, N, N, N, N, N, N },
					{ R, R, N, N, R, R, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, R, N, N, N, N, N, B, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, (3*2 + 3) + (3 + 3*2));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, B, N, N },
					{ N, N, N, N, N, N, N, N, B, B, N, N, N },
					{ N, N, N, N, B, N, N, W, W, N, N, N, N },
					{ N, N, R, W, N, N, R, R, N, N, N, N, N },
					{ N, W, W, N, N, R, R, N, N, N, N, N, N },
					{ R, B, N, N, R, R, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, B, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, (3 + 3*2) + (3*2 + 3*3));
			}
		}

		[UnityTest]
		public IEnumerator WildBlock_Complex_Diagonals()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ R, N, B, N, W, N, N, N, W, N, N, N, N },
					{ N, W, N, N, N, B, N, R, N, N, N, N, N },
					{ B, N, R, N, N, N, W, N, N, N, N, N, N },
					{ N, W, N, N, N, R, N, B, N, N, N, N, N },
					{ R, N, B, N, W, N, N, N, W, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, (3*4) + (3*6));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, R },
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
					{ N, N, N, N, N, N, N, N, N, N, R, N, N },
					{ N, N, N, N, N, N, N, N, N, W, N, N, N },
					{ N, N, N, N, N, N, N, N, R, N, N, N, N },
					{ N, N, N, N, N, N, N, W, N, N, N, N, N },
					{ N, N, N, N, N, N, R, N, N, N, N, N, N },
					{ N, N, N, N, N, W, N, N, N, N, N, N, N },
					{ N, N, N, N, R, N, N, N, N, N, N, N, N },
					{ N, N, N, W, N, N, N, N, N, N, N, N, N },
					{ N, N, R, N, N, N, N, N, N, N, N, N, N },
					{ N, W, N, N, N, N, N, N, N, N, N, N, N },
					{ R, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*11);
			}
		}

		[UnityTest]
		public IEnumerator WildBlock_Diagonals_Backtrack()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, B, N, N, N, N, N },
					{ N, N, N, N, B, N, B, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, N, N, N, N, N },
					{ N, N, W, N, W, N, N, N, N, N, N, N, N },
					{ N, R, N, R, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, R, N, R, W, N, B, B, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 3);
			}
		}

		[UnityTest]
		public IEnumerator WildBlock_Diagonals_Backtrack_Wrapped()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, B, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, R, W },
					{ N, N, N, N, N, N, N, N, N, N, N, R, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, R, N },
					{ N, B, B, N, N, N, N, N, N, N, N, R, W },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ B, N, N, N, N, N, N, N, N, N, N, N, N },
					{ B, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, N, N, W, B },
					{ N, N, N, N, N, N, N, N, N, N, R, W, N },
					{ N, N, N, N, N, N, N, N, N, N, R, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, R, N, N },
					{ B, B, N, N, N, N, N, N, N, N, R, W, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, B, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, R, W },
					{ N, N, N, N, N, N, N, N, N, N, N, R, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3*3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, R, W },
					{ N, N, N, N, N, N, N, N, N, N, R, R, N },
					{ N, N, N, N, N, N, N, N, N, N, W, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*3 + 3*3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, B, N, N, N, N, N, N, N, N, N, N, N },
					{ W, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ B, B, N, N, N, N, N, N, N, N, N, N, N },
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, N, W },
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 3*2 + 3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, N, N, N, N, N, N, N, N, N, N, N, N },
					{ B, N, N, N, N, N, N, N, N, N, N, N, B },
					{ N, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ W, N, N, N, N, N, N, N, N, N, N, N, B },
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 0);
			}
		}

		#endregion

		#region Special Blocks

		[UnityTest]
		public IEnumerator BlockSmite_Basics()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ R, B, G, N, N, N, G, N, N, N, N, N, N },
				{ Q, Q, Q, N, R, N, Q, N, N, G, N, Q, Q },
				{ Q, Q, Q, N, Q, N, B, N, Q, S, N, Q, Q },
				{ Q, Q, Q, N, R, N, B, G, S, S, N, Q, Q },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, G, N, N, N, N, N, N, N, N, N, N },
				{ Q, Q, Q, N, N, N, N, N, N, G, N, Q, Q },
				{ Q, Q, Q, N, N, N, N, N, Q, S, N, Q, Q },
				{ Q, Q, Q, N, N, N, G, G, S, S, N, Q, Q },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, 4 + 4);
		}

		[UnityTest]
		public IEnumerator RowSmite_Basics()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, H, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 1);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ G, B, R, N, N, N, N, N, N, N, N, N, R },
					{ R, H, G, N, N, N, N, N, N, N, N, N, G },
					{ R, B, G, N, N, N, N, N, N, N, N, N, G },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ G, B, R, N, N, N, N, N, N, N, N, N, R },
					{ R, B, G, N, N, N, N, N, N, N, N, N, G },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 4);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ G, B, R, N, N, N, N, N, N, N, N, N, R },
					{ R, H, H, H, H, H, N, N, N, N, N, N, G },
					{ R, B, G, N, N, N, N, N, N, N, N, N, G },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ G, B, R, N, N, N, N, N, N, N, N, N, R },
					{ R, B, G, N, N, N, N, N, N, N, N, N, G },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 7*5);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ G, B, R, H, H, H, N, N, N, N, N, N, R },
					{ R, H, H, H, H, H, N, N, N, N, N, N, G },
					{ R, B, G, H, H, H, N, N, N, N, N, N, G },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 1, 7*3 + 7*5 + 7*3);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ G, B, R, N, N, N, N, N, N, N, N, N, R },
					{ R, H, S, N, N, S, N, N, N, N, N, S, S },
					{ R, B, G, N, N, S, N, N, N, N, N, S, G },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, R, N, N, N, N, N, N, N, N, N, R },
					{ G, B, S, N, N, S, N, N, N, N, N, S, S },
					{ R, B, G, N, N, S, N, N, N, N, N, S, G },
				};

				yield return EvaluateGrid(blocks, blocksDone, 2, 2);
			}
		}


		[UnityTest]
		public IEnumerator SpecialBlocks_Mix()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, H, N, N, N, H, N, N, N, N, N },
				{ N, N, N, Q, N, N, N, Q, N, N, R, N, N },
				{ N, N, N, H, N, Q, N, W, N, N, R, W, N },
				{ N, Q, N, S, N, S, N, W, N, N, Q, Q, N },
				{ N, W, N, S, N, S, N, W, Q, W, Q, Q, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, Q, N, N, N, Q, N, N, R, N, N },
				{ N, Q, N, S, N, S, N, W, N, N, Q, Q, N },
				{ N, W, N, S, N, S, N, W, Q, W, Q, Q, N },
			};

			yield return EvaluateGrid(blocks, blocksDone, 2, 5 + 2*2);
		}

		#endregion

		#region Score Tests

		[UnityTest]
		public IEnumerator Score_Cascades()
		{
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 3, 3 + 3*2);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 3, 3 + 6*2);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 3, 6 + 6*2);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, G, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, B, N, N, N, N, N, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, N, N, N, N, N, N },
					{ G, R, G, G, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, 5, 6 + 6*2 + 6*3);
			}
		}

		[UnityTest]
		public IEnumerator Score_Rules()
		{
			//
			// Any
			//
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, G, N, N, N, N },
					{ N, N, N, B, N, N, N, G, N, N, N, N, N },
					{ R, R, R, B, N, N, G, S, S, S, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, (MatchScoringType)~0, 1, 3*3);
				Assert.AreEqual(3 * 3, m_MatchObjective.MatchesDone);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, G, N, N, N, N },
					{ N, N, N, B, N, N, N, G, N, N, N, N, N },
					{ R, R, R, B, N, N, G, S, S, S, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				};

				var matchAllTypes = MatchScoringType.Horizontal | MatchScoringType.Vertical | MatchScoringType.Diagonals;

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, matchAllTypes, 1, 3*3);
				Assert.AreEqual(3 * 3, m_MatchObjective.MatchesDone);
			}

			//
			// Single objective type
			//
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, G, N, N, N, N },
					{ N, N, N, B, N, N, N, G, N, N, N, N, N },
					{ R, R, R, B, N, N, G, S, S, S, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Horizontal, 1, 3*3);
				Assert.AreEqual(1, m_MatchObjective.MatchesDone);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, G, N, N, N, N },
					{ N, N, N, B, N, N, N, G, N, N, N, N, N },
					{ R, R, R, B, N, N, G, S, S, S, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Vertical, 1, 3*3);
				Assert.AreEqual(1, m_MatchObjective.MatchesDone);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, B, N, N, N, N, G, N, N, N, N },
					{ N, N, N, B, N, N, N, G, N, N, N, N, N },
					{ R, R, R, B, N, N, G, S, S, S, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Diagonals, 1, 3*3);
				Assert.AreEqual(1, m_MatchObjective.MatchesDone);
			}

			//
			// Single Objective Type - Longer
			//
			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ R, R, R, R, N, B, B, B, B, B, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Horizontal, 1, 3*2 + 3*3);
				Assert.AreEqual(2 + 3, m_MatchObjective.MatchesDone);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, B, N, N, N, N, N, N, N, N, N, N },
					{ R, N, B, N, N, N, N, N, N, N, N, N, N },
					{ R, N, B, N, N, N, N, N, N, N, N, N, N },
					{ R, N, B, N, N, N, N, N, N, N, N, N, N },
					{ R, N, B, N, N, N, N, N, N, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Vertical, 1, 3*2 + 3*3);
				Assert.AreEqual(2 + 3, m_MatchObjective.MatchesDone);
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
					{ N, N, N, N, B, N, N, N, N, N, N, N, N },
					{ N, N, N, R, N, B, N, N, N, N, N, N, N },
					{ N, N, R, N, N, N, B, N, N, N, N, N, N },
					{ N, R, N, N, N, N, N, B, N, N, N, N, N },
					{ R, N, N, N, N, N, N, N, B, N, N, N, N },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return EvaluateGrid(blocks, blocksDone, DefaultRules, MatchScoringType.Diagonals, 1, 3*2 + 3*3);
				Assert.AreEqual(2 + 3, m_MatchObjective.MatchesDone);
			}
		}

		#endregion

		#region Pinned Tests

		[UnityTest]
		public IEnumerator Pinned()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, G, N, R, N, N, N, N, N, N, N },
				{ N, N, B, N, N, R, N, N, N, N, N, N, N },
				{ N, N, N, R, N, N, N, N, N, N, N, N, N },
				{ N, R, B, R, N, B, N, N, N, N, N, G, N },
				{ R, N, N, G, N, B, N, N, R, N, N, B, N },
				{ N, B, G, N, N, R, N, G, N, N, N, B, N },
				{ R, B, G, G, N, R, N, B, B, B, N, B, N },
				{ N, N, N, N, N, N, N, N, N, G, N, N, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, G, N, R, N, N, N, N, N, N, N },
				{ N, N, N, R, N, R, N, N, N, N, N, N, N },
				{ N, N, B, R, N, B, N, N, N, N, N, N, N },
				{ N, R, B, N, N, B, N, N, N, N, N, N, N },
				{ R, B, G, G, N, N, N, N, N, N, N, N, N },
				{ R, B, G, G, N, R, N, N, N, N, N, N, N },
				{ N, N, N, N, N, R, N, G, R, G, N, G, N },
			};

			GridCoords[] pinned = new GridCoords[] {
				new GridCoords(1, 0),
				new GridCoords(1, 1),
				new GridCoords(1, 2),
				new GridCoords(1, 3),
				new GridCoords(4, 3),

				new GridCoords(3, 5),

				new GridCoords(1, 7),
				new GridCoords(1, 8),
			};

			var startGrid = new BlocksGrid(MaxRows, MaxColumns);
			yield return startGrid.ApplyActions(new GridAction[] { SetupPlaceAction(blocks) });

			m_Grid = new BlocksGrid(startGrid, MaxRows, MaxColumns, pinned);
			m_ScoreGrid = new ScoreGrid(MaxRows, MaxColumns, DefaultRules);

			m_Grids = new GameGrid[] { m_Grid, m_ScoreGrid };

			m_SkipGridSetup = true;
			yield return EvaluateGrid(blocks, blocksDone, 2, 6, false);
		}

		#endregion
	}
}