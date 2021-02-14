using System;
using TetrisTower.Logic;

namespace TetrisTower.Levels
{
	[Serializable]
	public class LevelData
	{
		public BlocksShape NextShape;
		public BlocksShape FallingShape;
		public int FallingColumn;

		public GridShapeTemplate[] ShapeTemplates;

		public BlocksGrid Grid;
	}
}