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

		public IEnumerator PlaceShape(GridCoords placedCoords, BlocksShape placedShape)
		{
			foreach (var pair in placedShape.ShapeCoords) {
				var coords = placedCoords + pair.Coords;

				// Hitting the limit, won't be stored.
				if (coords.Row >= Rows)
					continue;

				UnityEngine.Debug.Assert(this[coords] == null);

				this[coords] = pair.Value;
			}

			yield break;
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