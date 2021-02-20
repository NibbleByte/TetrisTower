using System;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Logic
{
	public class GridGroup<T>
	{
		public int Rows => m_Rows;
		public int Columns => m_Columns;

		public T this[int row, int column] {
			get => m_Blocks[row * Columns + column];
			set => m_Blocks[row * Columns + column] = value;
		}

		public T this[GridCoords coords] {
			get => m_Blocks[coords.Row * Columns + coords.Column];
			set => m_Blocks[coords.Row * Columns + coords.Column] = value;
		}

		public void SetSize(int rows, int columns)
		{
			m_Rows = rows;
			m_Columns = columns;

			m_Blocks = new T[rows * columns];
		}

		[SerializeField] private int m_Rows;
		[SerializeField] private int m_Columns;
		[SerializeField] private T[] m_Blocks;
	}

	public class GridShape<T>
	{
		[Serializable]
		public struct ShapeBind
		{
			public GridCoords Coords;
			public T Value;

			public override string ToString()
			{
				return $"{Coords} {Value}";
			}
		}
		public static ShapeBind Bind(GridCoords coords, T value) => new ShapeBind() { Coords = coords, Value = value };

		public List<ShapeBind> ShapeCoords = new List<ShapeBind>();

		public int Rows => GetSize(GridCoords.GetRow);
		public int Columns => GetSize(GridCoords.GetColumn);

		public IEnumerable<ShapeBind> AddToCoordsWrapped(GridCoords addedCoords, GameGrid grid)
		{
			foreach(var shapeCoords in ShapeCoords) {
				var resultBind = shapeCoords;

				resultBind.Coords += addedCoords;
				resultBind.Coords.WrapColumn(grid);

				yield return resultBind;
			}
		}

		private int GetSize(Func<GridCoords, int> selector) {
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach(var pair in ShapeCoords) {
				if (selector(pair.Coords) < min) min = selector(pair.Coords);
				if (selector(pair.Coords) > max) max = selector(pair.Coords);
			}

			return max - min + 1;
		}
	}
}