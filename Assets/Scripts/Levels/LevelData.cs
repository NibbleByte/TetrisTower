using System;
using TetrisTower.Logic;

namespace TetrisTower.Levels
{
	[Serializable]
	public class LevelData
	{
		public BlocksShape NextShape;
		public BlocksShape FallingShape;

		public GridCoords FallingCoords;
		public float FallSpeed = 2f;

		public GridShapeTemplate[] ShapeTemplates;

		public BlocksGrid Grid;
	}
}