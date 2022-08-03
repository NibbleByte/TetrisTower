using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TetrisTower.Game;
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

		private GameConfig m_GameConfig;

		private BlockType R; // "Red" block
		private BlockType G; // "Green" block
		private BlockType B; // "Blue" block

		private BlockType W; // "Wild" block

		private BlockType S; // "Static" block

		private BlockType N; // "null" block

		private const int MaxRows = 13;
		private const int MaxColumns = 13;

		private static readonly GridRules DefaultRules = new GridRules {
			MatchHorizontalLines = 3,
			MatchVerticalLines = 3,
			MatchDiagonalsLines = 3,

			ObjectiveType = (MatchScoringType)~0,

			WrapSidesOnMatch = true,
			WrapSidesOnMove = true,
		};

		private BlocksGrid m_Grid;


		#region Setup

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			m_GameConfig = GameConfig.FindDefaultConfig();
			if (m_GameConfig == null)
				throw new System.NotSupportedException();

			R = m_GameConfig.DefaultBlocksSet.Blocks[0];
			G = m_GameConfig.DefaultBlocksSet.Blocks[1];
			B = m_GameConfig.DefaultBlocksSet.Blocks[2];

			W = m_GameConfig.DefaultBlocksSet.WildBlock;
			S = m_GameConfig.DefaultBlocksSet.WonBonusBlock;

			N = null;
		}

		// This is called before every test in this class
		//[SetUp]
		//public void Setup()
		//{
		//	Debug.Log("Setup");
		//}

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

		private IEnumerator SetupGrid(BlockType[,] blocks)
		{
			Assert.LessOrEqual(blocks.GetLength(0), MaxRows, "Wrong rows count.");
			Assert.LessOrEqual(blocks.GetLength(1), MaxColumns, "Wrong columns count.");

			m_Grid = new BlocksGrid(MaxRows, MaxColumns);

			yield return m_Grid.ApplyActions(new GridAction[] { SetupPlaceAction(blocks) });
		}

		[TearDown]
		public void TearDown()
		{
			m_Grid = null;
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
				if (block == S) return nameof(S);

				if (block == N) return nameof(N);

				throw new System.NotSupportedException(block?.name);
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

		private void AssertGrid(BlockType[,] blocks)
		{
			for (int row = 0; row < MaxRows; ++row) {

				for (int column = 0; column < blocks.GetLength(1); ++column) {
					int blocksRow = blocks.GetLength(0) - 1 - row;

					BlockType block = blocksRow >= 0 ? blocks[blocksRow, column] : null;
					BlockType gridBlock = m_Grid[row, column];

					if (block != gridBlock) {
						PrintGrid();
						System.Diagnostics.Debugger.Break();
					}

					Assert.AreEqual(block, m_Grid[row, column], $"Blocks[{blocksRow}, {column}] != Grid[{row}, {column}]");
				}
			}
		}

		private void AssertNoActions(GridRules rules)
		{
			List<GridAction> pendingActions = GameGridEvaluation.Evaluate(m_Grid, rules);

			if (pendingActions.Count > 0) {
				PrintGrid();
				System.Diagnostics.Debugger.Break();
			}

			Assert.AreEqual(0, pendingActions.Count, $"Grid Actions left: {pendingActions.Count}.");
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

			yield return SetupGrid(blocks);

			Assert.AreEqual(MaxRows, m_Grid.Rows);
			Assert.AreEqual(MaxColumns, m_Grid.Columns);

			AssertGrid(blocks);

			yield return SetupGrid(blocks2);

			AssertGrid(blocks2);

			yield return SetupGrid(blocks9);

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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
		}

		[UnityTest]
		public IEnumerator Basics_Diagonals1()
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
		}

		[UnityTest]
		public IEnumerator Basics_Diagonals2()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, R, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, R, N, N, N, N, N, N, N, N, B, G },
				{ G, N, N, R, N, B, N, N, N, S, B, R, B },
				{ B, G, N, N, G, N, B, R, N, S, S, B, R },
				{ R, B, N, N, N, G, N, B, R, S, S, S, B },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, S, N, N, N },
				{ N, N, N, N, N, N, N, N, N, S, S, N, N },
				{ N, N, N, N, G, G, N, R, R, S, S, S, N },
			};

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
		}

		[UnityTest]
		public IEnumerator Basics_MaxMatch()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ R, N, N, N, B, N, N, N, S, N, N, N, N },
				{ R, R, N, B, B, B, N, S, S, S, N, N, R },
				{ R, R, N, B, B, B, N, S, S, S, N, N, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, S, N, N, N, N },
				{ N, N, N, N, N, N, N, S, S, S, N, N, N },
				{ N, N, N, N, N, N, N, S, S, S, N, N, N },
			};

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
		}

		[UnityTest]
		public IEnumerator Basics_WholeRow()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ R, R, R, R, R, R, R, R, R, R, R, R, R },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
			};

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
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

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, W, N },
					{ N, S, S, N, N, N, N, N, N, N, R, S, N },
					{ S, B, B, N, N, N, N, N, N, N, S, R, S },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);


				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, B, N, N, N, N, N, N, N, N, R, W },
					{ W, B, N, N, N, N, N, N, N, N, N, R, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);

				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, N, N, N, N, N, N, N, N, R, R, W },
					{ W, B, N, N, N, N, N, N, N, N, W, R, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);

				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ W, B, N, N, N, N, N, N, N, N, N, N, W },
					{ W, B, N, N, N, N, N, N, N, N, N, W, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);

				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ B, B, N, N, N, N, N, N, N, N, N, N, W },
					{ B, B, N, N, N, N, N, N, N, N, N, W, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);

				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}

			{
				BlockType[,] blocks = new BlockType[,] {
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				BlockType[,] blocksDone = new BlockType[,] {
					{ B, N, N, N, N, N, N, N, N, N, N, N, W },
				};

				yield return SetupGrid(blocks);

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(blocksDone);

				//
				// Mirrored
				//

				yield return SetupGrid(Mirror(blocks));

				yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

				AssertNoActions(DefaultRules);
				AssertGrid(Mirror(blocksDone));
			}
		}

		#endregion
	}
}