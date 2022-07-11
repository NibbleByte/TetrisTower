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

		public BlockType this[int row, int column] => m_Blocks[row * Columns + column];

		public BlockType this[GridCoords coords] {
			get => m_Blocks[coords.Row * Columns + coords.Column];
			private set => m_Blocks[coords.Row * Columns + coords.Column] = value;
		}


		[UnityEngine.SerializeField] private int m_Rows;
		[UnityEngine.SerializeField] private int m_Columns;
		[UnityEngine.SerializeField] private BlockType[] m_Blocks;

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

				UnityEngine.Debug.Assert(this[pair.Coords] == null);

				this[pair.Coords] = pair.Value;
			}

			yield break;
		}

		private IEnumerator ClearMatchedCells(ClearMatchedAction action)
		{
			foreach(var coord in action.Coords) {
				UnityEngine.Debug.Assert(this[coord] != null);

				this[coord] = null;
			}

			yield break;
		}

		private IEnumerator MoveCells(MoveCellsAction action)
		{
			foreach(var movedCell in action.MovedCells) {
				UnityEngine.Debug.Assert(this[movedCell.Key] != null);
				UnityEngine.Debug.Assert(this[movedCell.Value] == null);

				this[movedCell.Value] = this[movedCell.Key];
				this[movedCell.Key] = null;
			}

			yield break;
		}
	}
}