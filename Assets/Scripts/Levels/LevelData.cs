using System;
using TetrisTower.Logic;

namespace TetrisTower.Levels
{
	[Serializable]
	public class LevelData
	{
		public BlocksShape NextShape;
		public BlocksShape FallingShape;


		public int FallingColumn;		// The first (leftest) column that the shape is falling at the moment.
		public float FallDistance = 0f;	// Distance passed while falling.
		public float FallSpeed = 2f;	// Speed of falling.

		public GridShapeTemplate[] ShapeTemplates;

		public BlocksGrid Grid;
	}
}