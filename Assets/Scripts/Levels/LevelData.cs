using System;
using TetrisTower.Logic;

namespace TetrisTower.Levels
{
	[Serializable]
	public class LevelData
	{
		public BlocksShape NextShape;
		public BlocksShape FallingShape;


		public int FallingColumn;					// The first (leftest) column that the shape is falling at the moment.
		public float FallDistanceNormalized = 0f;	// Normalized distance passed while falling. 1.0 is one tile distance.
		public float FallSpeedNormalized = 2f;		// Speed of falling.

		public GridShapeTemplate[] ShapeTemplates;

		public BlocksGrid Grid;
	}
}