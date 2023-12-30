using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	/// <summary>
	/// Bonus actions are executed after placing blocks.
	/// They usually are evaluated right when executed.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public interface BonusAction
	{
		GridAction[] GenerateActions(GridLevelData levelData);
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(BonusAction), true)]
	public class BonusActionDrawer : SerializeReferenceCreatorDrawer<BonusAction>
	{
	}
#endif

	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PushUpLine_BonusAction : BonusAction
	{
		public GridAction[] GenerateActions(GridLevelData levelData)
		{
			Vector2Int blocksRange = levelData.SpawnBlockTypesRange;
			List<BlockType> feasibleBlocks = new List<BlockType>();

			var pushBlocks = new List<KeyValuePair<int, BlockType>>();

			for (int column = 0; column < levelData.Grid.Columns; ++column) {

				// Populate the pool with blocks that won't match with the ones around.
				feasibleBlocks.Clear();
				for(int typeIndex = blocksRange.x; typeIndex < blocksRange.y; ++typeIndex) {
					BlockType feasibleBlock = BlockType.B1 + typeIndex;

					bool match = HasPotentialMatch(feasibleBlock, levelData, column, 0)
						|| HasPotentialMatch(feasibleBlock, levelData, column, +1)
						|| HasPotentialMatch(feasibleBlock, levelData, column, -1)
						;
					if (match) {
						continue;
					}

					// Check the neighbour push up blocks too.
					if (HasPotentialPushMatchClamped(feasibleBlock, pushBlocks, levelData, column, -1))
						continue;

					// Wrap around check. Column is -1 so it checks 0, 1 and so on.
					if (levelData.Rules.WrapSidesOnMatch && column == levelData.Grid.Columns - 1 && HasPotentialPushMatchClamped(feasibleBlock, pushBlocks, levelData, -1, +1))
						continue;

					// Wrap around check.
					if (levelData.Rules.WrapSidesOnMatch && column == levelData.Grid.Columns - 1 && feasibleBlock == pushBlocks[column - 1].Value && feasibleBlock == pushBlocks[0].Value)
						continue;

					feasibleBlocks.Add(feasibleBlock);
				};

				// Can't select block type that won't match when pushed up this column.
				// Skipping column may cause some matches, so we insert some future block type.
				if (feasibleBlocks.Count == 0) {
					feasibleBlocks.Add(BlockType.B1 + blocksRange.y);
				}

				BlockType selectedBlockType = feasibleBlocks[levelData.Random.Next(feasibleBlocks.Count)];
				pushBlocks.Add(KeyValuePair.Create(column, selectedBlockType));
			}

			return new GridAction[] { new PushUpCellsAction() { PushBlocks = pushBlocks } };
		}

		private static bool HasPotentialMatch(BlockType feasibleBlock, GridLevelData levelData, int column, int direction)
		{
			BlocksGrid grid = levelData.Grid;

			int matchLength = direction switch {
				-1 => levelData.Rules.MatchDiagonalsLines,
				0 => levelData.Rules.MatchVerticalLines,
				1 => levelData.Rules.MatchDiagonalsLines,
				_ => throw new System.NotSupportedException(),
			};

			// Check the first n rows in direction up, left and right diagonal.
			for(int row = 0; row < matchLength - 1; ++row) {
				BlockType gridBlock = grid[row, MathUtils.WrapValue(column + direction * (row + 1), grid.Columns)];

				if (gridBlock != feasibleBlock && gridBlock != BlockType.SpecialWildBlock)
					return false;
			}

			return true;
		}

		private static bool HasPotentialPushMatchClamped(BlockType feasibleBlock, List<KeyValuePair<int, BlockType>> pushBlocks, GridLevelData levelData, int column, int direction)
		{
			int matchLength = levelData.Rules.MatchHorizontalLines;

			// Check the first n rows in direction up, left and right diagonal.
			for(int i = 0; i < matchLength - 1; ++i) {
				int matchColumn = column + direction * (i + 1);

				// Those cases are handled separately.
				if (matchColumn < 0 || matchColumn >= levelData.Grid.Columns)
					return false;

				// Careful! Future columns may not be decided yet!
				var pair = pushBlocks[matchColumn];

				if (pair.Value != feasibleBlock)
					return false;
			}

			return true;
		}
	}
}