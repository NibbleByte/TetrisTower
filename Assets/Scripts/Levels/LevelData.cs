using Newtonsoft.Json;
using System;
using TetrisTower.Logic;

namespace TetrisTower.Levels
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class LevelData
	{
		public BlocksShape NextShape;
		public BlocksShape FallingShape;

		// Because of Unity serialization, FallingShape may never be null :(
		public bool HasFallingShape => FallingShape != null && FallingShape.ShapeCoords.Count > 0;


		public int FallingColumn;					// The first (leftest) column that the shape is falling at the moment.
		public float FallDistanceNormalized = 0f;	// Normalized distance passed while falling. 1.0 is one tile distance.
		public float FallSpeedNormalized = 2f;		// Speed of falling.

		public GridShapeTemplate[] ShapeTemplates;
		public BlockType[] SpawnedBlocks;

		public GridRules Rules;
		public BlocksGrid Grid;

		public GridCoords FallShapeCoords => new GridCoords(Grid.Rows - (int)Math.Ceiling(FallDistanceNormalized), FallingColumn);
	}
}