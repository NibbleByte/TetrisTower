using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum TowerLevelRunningState
	{
		Running,
		Won,
		Lost,
	}

	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class GridLevelData
	{
		public SceneReference BackgroundScene;

		[SerializeReference] public BlocksShape NextShape;
		[SerializeReference] public BlocksShape FallingShape;


		public int FallingColumn;					// The first (leftest) column that the shape is falling at the moment.
		public float FallDistanceNormalized = 0f;	// Normalized distance passed by the lowest block of the shape while falling. 1.0 is one tile distance.
		public float FallSpeedNormalized = 2f;		// Speed of falling.

		public GridShapeTemplate[] ShapeTemplates;
		public BlockType[] SpawnedBlocks;

		[Tooltip("Read-only. Used for debug.")]
		public int RandomInitialSeed;
		public System.Random Random = new System.Random();

		public GridRules Rules;
		public BlocksGrid Grid;

		[SerializeReference]
		public ScoreGrid Score;

		public int ClearBlocksEndCount;

		public int ClearBlocksRemainingCount => Mathf.Max(ClearBlocksEndCount - Score.TotalClearedBlocksCount, 0);

		public TowerLevelRunningState RunningState = TowerLevelRunningState.Running;
		public bool IsPlaying => RunningState == TowerLevelRunningState.Running;

		public GridCoords CalcFallShapeCoordsAt(int column) => new GridCoords(Grid.Rows - (int)Math.Ceiling(FallDistanceNormalized), column);
		public GridCoords FallShapeCoords => CalcFallShapeCoordsAt(FallingColumn);
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(GridLevelData))]
	public class TowerLevelDataDrawer : Tools.SerializeReferenceCreatorDrawer<GridLevelData>
	{
	}
#endif
}