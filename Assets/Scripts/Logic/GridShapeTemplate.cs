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
	}

	public class GridShapeTemplateConverter : SerializableAssetConverter<GridShapeTemplate>
	{
		public GridShapeTemplateConverter(AssetsRepository repository) : base(repository) { }
	}
}