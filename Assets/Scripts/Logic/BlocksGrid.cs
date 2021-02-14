using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace TetrisTower.Logic
{
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

		// For serialization.
		public BlocksGrid()
		{
		}

		public BlocksGrid(int rows, int columns)
		{
			m_Blocks = new BlockType[rows * columns];
		}

		public void PlaceCells(IReadOnlyCollection<KeyValuePair<GridCoords, BlockType>> placeCells)
		{
			foreach (var pair in placeCells) {
				UnityEngine.Debug.Assert(this[pair.Key] == null);

				this[pair.Key] = pair.Value;
			}
		}

		public IEnumerator ClearMatchedCells(IReadOnlyCollection<GridCoords> coords)
		{
			foreach(var coord in coords) {
				UnityEngine.Debug.Assert(this[coord] != null);

				this[coord] = null;
			}

			yield break;
		}

		public IEnumerator MoveCells(IReadOnlyCollection<KeyValuePair<GridCoords, GridCoords>> movedCells)
		{
			foreach(var movedCell in movedCells) {
				UnityEngine.Debug.Assert(this[movedCell.Key] != null);
				UnityEngine.Debug.Assert(this[movedCell.Value] == null);

				this[movedCell.Value] = this[movedCell.Key];
				this[movedCell.Key] = null;
			}

			yield break;
		}
	}
}