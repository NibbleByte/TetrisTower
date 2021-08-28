using System;
using System.Collections.Generic;
using System.Linq;
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

		public ShapeBind[] ShapeCoords = new ShapeBind[0];

		public int Rows => GetSize(GridCoords.GetRow);
		public int Columns => GetSize(GridCoords.GetColumn);

		public int MinRow => GetMin(GridCoords.GetRow);
		public int MaxRow => GetMax(GridCoords.GetRow);

		public int MinColumn => GetMin(GridCoords.GetColumn);
		public int MaxColumn => GetMax(GridCoords.GetColumn);

		public IEnumerable<ShapeBind> AddToCoordsWrapped(GridCoords addedCoords, GameGrid grid)
		{
			foreach(var shapeCoords in ShapeCoords) {
				var resultBind = shapeCoords;

				resultBind.Coords += addedCoords;
				resultBind.Coords.WrapColumn(grid);

				yield return resultBind;
			}
		}

		private int GetMax(Func<GridCoords, int> selector)
		{
			int max = int.MinValue;
			foreach (var pair in ShapeCoords) {
				if (selector(pair.Coords) > max) max = selector(pair.Coords);
			}

			return max;
		}

		private int GetMin(Func<GridCoords, int> selector) {
			int min = int.MaxValue;
			foreach(var pair in ShapeCoords) {
				if (selector(pair.Coords) < min) min = selector(pair.Coords);
			}

			return min;
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