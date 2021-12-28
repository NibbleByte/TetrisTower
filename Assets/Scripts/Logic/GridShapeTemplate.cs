using System;
using System.Collections.Generic;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CreateAssetMenu(fileName = "Unknown_GridShapeTemplate", menuName = "Tetris Tower/Shape Template")]
	public class GridShapeTemplate : SerializableAsset
	{
		public List<GridCoords> ShapeTemplate;

		public int Rows => GetSize(GridCoords.GetRow);
		public int Columns => GetSize(GridCoords.GetColumn);

		public int MinRow => GetMin(GridCoords.GetRow);
		public int MaxRow => GetMax(GridCoords.GetRow);

		public int MinColumn => GetMin(GridCoords.GetColumn);
		public int MaxColumn => GetMax(GridCoords.GetColumn);

		public GridCoords MinCoords => new GridCoords(MinRow, MinColumn);
		public GridCoords MaxCoords => new GridCoords(MaxRow, MaxColumn);

		private int GetMax(Func<GridCoords, int> selector)
		{
			int max = int.MinValue;
			foreach (var coords in ShapeTemplate) {
				if (selector(coords) > max) max = selector(coords);
			}

			return max;
		}

		private int GetMin(Func<GridCoords, int> selector)
		{
			int min = int.MaxValue;
			foreach (var coords in ShapeTemplate) {
				if (selector(coords) < min) min = selector(coords);
			}

			return min;
		}

		private int GetSize(Func<GridCoords, int> selector)
		{
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (var coords in ShapeTemplate) {
				if (selector(coords) < min) min = selector(coords);
				if (selector(coords) > max) max = selector(coords);
			}

			return max - min + 1;
		}
	}

	public class GridShapeTemplateConverter : SerializableAssetConverter<GridShapeTemplate>
	{
		public GridShapeTemplateConverter(AssetsRepository repository) : base(repository) { }
	}
}