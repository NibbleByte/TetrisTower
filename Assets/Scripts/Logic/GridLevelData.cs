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

		// Initial spawn blocks from the array.
		public int InitialSpawnBlockTypesCount = 3;

		// Spawn blocks from the array in range [0, TypesCount).
		public int SpawnBlockTypesCount = 3;

		// Every x matches done by the player, increase the range of the types count.
		public int ScorePerAdditionalSpawnBlockType = 50;


		[Tooltip("Read-only. Used for debug.")]
		public int RandomInitialSeed;
		public System.Random Random = new System.Random();

		public GridRules Rules;
		public BlocksGrid Grid;

		// The valid size where blocks can be placed by the player. It should be smaller than the grid itself.
		public GridCoords PlayableSize;

		[SerializeReference]
		public ScoreGrid Score;

		public int ObjectiveEndCount;

		public int ObjectiveRemainingCount => Mathf.Max(ObjectiveEndCount - Score.ObjectiveProgress, 0);

		public TowerLevelRunningState RunningState = TowerLevelRunningState.Running;
		public bool IsPlaying => RunningState == TowerLevelRunningState.Running;

		// Use Math.Floor() + 1, instead of Math.Ceiling() as initial value may be exactly 0 giving bad results.
		public GridCoords CalcFallShapeCoordsAt(int column) => new GridCoords(Grid.Rows - (int)Math.Floor(FallDistanceNormalized) - 1, column);
		public GridCoords FallShapeCoords => CalcFallShapeCoordsAt(FallingColumn);

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (Rules.ObjectiveType == 0) {
				Debug.LogError($"{nameof(GridLevelData)} has no ObjectiveType set.", context);
			}

			for (int i = 0; i < SpawnedBlocks.Length; i++) {
				var block = SpawnedBlocks[i];

				if (block == null) {
					Debug.LogError($"Missing {i}-th block from {nameof(SpawnedBlocks)} in this {nameof(GridLevelData)} \"{BackgroundScene}\". {context}", context);
					continue;
				}

				if (!repo.IsRegistered(block)) {
					Debug.LogError($"Block {block.name} is not registered for serialization, from {nameof(SpawnedBlocks)} in this {nameof(GridLevelData)} \"{BackgroundScene}\". {context}", context);
				}
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(GridLevelData))]
	public class TowerLevelDataDrawer : Tools.SerializeReferenceCreatorDrawer<GridLevelData>
	{
	}
#endif
}