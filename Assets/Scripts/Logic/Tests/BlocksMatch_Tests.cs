using System.Collections;
using System.Collections.Generic;
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

		private const int MaxRows = 9;
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

		private void AssertGrid(BlockType[,] blocks)
		{
			for (int row = 0; row < MaxRows; ++row) {

				for (int column = 0; column < blocks.GetLength(1); ++column) {
					int blocksRow = blocks.GetLength(0) - 1 - row;

					BlockType block = blocksRow >= 0 ? blocks[blocksRow, column] : null;
					BlockType gridBlock = m_Grid[row, column];

					if (block != gridBlock) {
						System.Diagnostics.Debugger.Break();
					}

					Assert.AreEqual(block, m_Grid[row, column], $"Blocks[{blocksRow}, {column}] != Grid[{row}, {column}]");
				}
			}
		}

		private void AssertNoActions(GridRules rules)
		{
			List<GridAction> pendingActions = GameGridEvaluation.Evaluate(m_Grid, rules);
			Assert.AreEqual(0, pendingActions.Count, $"Grid Actions left - {pendingActions.Count}.");
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
				{ R, R, W, B, W, N, N, N, S, S, N, N, N },
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
		public IEnumerator WildBlock_Complex()
		{
			BlockType[,] blocks = new BlockType[,] {
				{ W, R, W, R, W, R, W, R, W, R, W, R, W },
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
				{ W, N, B, N, N, N, N, N, N, N, N, N, N },
				{ R, N, B, N, N, N, N, N, N, N, N, N, N },
				{ R, R, W, N, N, N, N, N, N, N, N, N, N },
			};

			BlockType[,] blocksDone = new BlockType[,] {
				{ N, N, N, N, N, N, N, N, N, N, N, N, N },
			};

			yield return SetupGrid(blocks);

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			yield return m_Grid.ApplyActions(GameGridEvaluation.Evaluate(m_Grid, DefaultRules));

			AssertNoActions(DefaultRules);
			AssertGrid(blocksDone);
		}

		#endregion
	}
}