using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TetrisTower.Logic
{
	/// <summary>
	/// Logical grid that marks occupied cells.
	/// Rows from bottom to top go from 0 to n (up direction).
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class BlocksGrid : GameGrid
	{
		public int Rows => m_Rows;
		public int Columns => m_Columns;

		public BlockType this[int row, int column] {
			get => m_Blocks[row * Columns + column];
			private set => m_Blocks[row * Columns + column] = value;
		}

		public BlockType this[GridCoords coords] {
			get => m_Blocks[coords.Row * Columns + coords.Column];
			private set => m_Blocks[coords.Row * Columns + coords.Column] = value;
		}


		[UnityEngine.SerializeField] private int m_Rows;
		[UnityEngine.SerializeField] private int m_Columns;
		[UnityEngine.SerializeField] private BlockType[] m_Blocks;

		[UnityEngine.Tooltip("Pinned blocks will stay up in the air until cleared.")]
		[UnityEngine.SerializeField] private List<GridCoords> m_PinnedCoords = new List<GridCoords>();

		// Used for debug / cheat.
		[NonSerialized]
		public bool MatchingFrozen = false;

		// For serialization.
		public BlocksGrid()
		{
		}

		public BlocksGrid(int rows, int columns)
		{
			m_Blocks = new BlockType[rows * columns];
			m_Rows = rows;
			m_Columns = columns;
		}

		public BlocksGrid(BlocksGrid grid)
		{
			m_Rows = grid.Rows;
			m_Columns = grid.Columns;
			m_Blocks = new BlockType[grid.m_Blocks.Length];
			m_PinnedCoords = grid.m_PinnedCoords.ToList();

			grid.m_Blocks.CopyTo(m_Blocks, 0);
		}

		public BlocksGrid(BlocksGrid grid, int rows, int columns, IEnumerable<GridCoords> pinnedCoords)
		{
			m_Rows = rows;
			m_Columns = columns;
			m_Blocks = new BlockType[rows * columns];
			m_PinnedCoords = pinnedCoords.ToList();

			grid.m_Blocks.CopyTo(m_Blocks, 0);
		}

		public IEnumerable<GridCoords> EnumerateOccupiedCoords()
		{
			for(int row = 0; row < Rows; ++row) {
				for(int column = 0; column < Columns; ++column) {
					if (this[row, column] != BlockType.None) {
						yield return new GridCoords(row, column);
					}
				}
			}
		}

		public IEnumerable<GridCoords> EnumerateFreeCoords()
		{
			for(int row = 0; row < Rows; ++row) {
				for(int column = 0; column < Columns; ++column) {
					if (this[row, column] == BlockType.None) {
						yield return new GridCoords(row, column);
					}
				}
			}
		}

		public bool IsPinnedCoords(GridCoords coords) => m_PinnedCoords.Contains(coords);

		private bool UnpinCoords(GridCoords coords) => m_PinnedCoords.Remove(coords);

		public IEnumerator ApplyActions(IEnumerable<GridAction> actions)
		{
			foreach (var action in MergeActions(actions)) {
				switch(action) {
					case PlaceAction placeAction:
						yield return PlaceShape(placeAction);
						break;
					case ClearMatchedAction clearAction:
						yield return ClearMatchedCells(clearAction);
						break;
					case MoveCellsAction moveAction:
						yield return MoveCells(moveAction);
						break;
					case PushUpCellsAction pushUpAction:
						yield return PushUpCells(pushUpAction);
						break;
				}
			}
		}

		private IEnumerable<GridAction> MergeActions(IEnumerable<GridAction> actions)
		{
			List<GridCoords> clearedBlocks = new List<GridCoords>();

			foreach (var action in actions) {

				switch (action) {
					case ClearMatchedAction clearAction:
						clearedBlocks.AddRange(clearAction.Coords);
						break;

					default:
						yield return action;
						break;
				}
			}

			// No duplicate clears.
			if (clearedBlocks.Count > 0) {
				yield return new ClearMatchedAction() { Coords = clearedBlocks.Distinct().ToArray() };
			}
		}

		private IEnumerator PlaceShape(PlaceAction action)
		{
			foreach (var pair in action.PlacedShape.AddToCoordsWrapped(action.PlaceCoords, this)) {

				// Hitting the limit, won't be stored.
				if (pair.Coords.Row >= Rows)
					continue;

				if (this[pair.Coords] != BlockType.None) {
					UnityEngine.Debug.LogError($"Placing block {pair.Value} in grid at {pair.Coords}, which is already taken by {this[pair.Coords]}");
				}

				this[pair.Coords] = pair.Value;
			}

			yield break;
		}

		private IEnumerator ClearMatchedCells(ClearMatchedAction action)
		{
			foreach(var coord in action.Coords) {
				UnityEngine.Debug.Assert(this[coord] != BlockType.None);

				this[coord] = BlockType.None;

				UnpinCoords(coord);
			}

			yield break;
		}

		private IEnumerator MoveCells(MoveCellsAction action)
		{
			foreach(var movedCell in action.MovedCells) {
				UnityEngine.Debug.Assert(this[movedCell.Key] != BlockType.None);
				UnityEngine.Debug.Assert(this[movedCell.Value] == BlockType.None);

				this[movedCell.Value] = this[movedCell.Key];
				this[movedCell.Key] = BlockType.None;
			}

			yield break;
		}

		private IEnumerator PushUpCells(PushUpCellsAction action)
		{
			foreach(var pair in action.PushBlocks) {
				int column = pair.Key;

				// Go up finding the first empty row, as there may be pinned blocks in mid-air.
				// Pinned blocks will be pushed up by other blocks.
				int row = 0;
				while (row < Rows - 1 && this[row, column] != BlockType.None) {
					row++;
				}

				if (row == Rows - 1 && this[Rows - 1, column] != BlockType.None) {
					UnityEngine.Debug.LogError($"Pushing blocks in grid at {column} column, which is full!");
				}

				// Now go down moving up all the rows.
				for(--row; row >= 0; --row) {
					this[row + 1, column] = this[row, column];
					UnpinCoords(new GridCoords(row, column));
				}

				this[0, column] = pair.Value;
			}

			yield break;
		}
	}
}