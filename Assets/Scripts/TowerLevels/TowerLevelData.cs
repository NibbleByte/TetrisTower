using Newtonsoft.Json;
using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class TowerLevelData
	{
		[SerializeReference] public BlocksShape NextShape;
		[SerializeReference] public BlocksShape FallingShape;


		public int FallingColumn;					// The first (leftest) column that the shape is falling at the moment.
		public float FallDistanceNormalized = 0f;	// Normalized distance passed while falling. 1.0 is one tile distance.
		public float FallSpeedNormalized = 2f;		// Speed of falling.

		public GridShapeTemplate[] ShapeTemplates;
		public BlockType[] SpawnedBlocks;

		public GridRules Rules;
		public BlocksGrid Grid;

		public GridCoords CalcFallShapeCoordsAt(int column) => new GridCoords(Grid.Rows - (int)Math.Ceiling(FallDistanceNormalized), column);
		public GridCoords FallShapeCoords => CalcFallShapeCoordsAt(FallingColumn);
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(TowerLevelData))]
	public class TowerLevelDataDrawer : Tools.SerializeReferenceCreatorDrawer<TowerLevelData>
	{
	}
#endif
}