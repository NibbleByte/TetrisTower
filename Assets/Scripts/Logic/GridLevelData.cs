using DevLocker.GFrame.Utils;
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

	public enum ModifyBlocksRangeType
	{
		AddRemoveAlternating = 0,
		AddBlocks = 4,
	}

	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class GridLevelData
	{
		public SceneReference BackgroundScene;

		// Rows from bottom to top go from 0 to n (up direction).
		[SerializeReference] public BlocksShape NextShape;
		[SerializeReference] public BlocksShape FallingShape;


		public int FallingColumn;					// The first (leftest) column that the shape is falling at the moment.
		public float FallDistanceNormalized = 0f;	// Normalized distance passed by the lowest block of the shape while falling. 1.0 is one tile distance.
		public float FallSpeedNormalized = 2f;		// Speed of falling.
		public float FallSpeedupPerAction = 0.01f;  // Added to the falling speed every placement.

		public float PlayTime = 0;					// In seconds;

		public GridShapeTemplate[] ShapeTemplates;
		public BlocksSkinSet BlocksSkinSet;
		public BlockSkin[] NormalBlocksSkins => BlocksSkinSet.GetNormalBlocks();

		// Initial spawn blocks from the array.
		public int InitialSpawnBlockTypesCount = 3;

		// Spawn blocks from the array in range [0, TypesCount).
		public Vector2Int SpawnBlockTypesRange = new Vector2Int(0, 3);

		[Tooltip("Every x matches done by the player, increase the range of the spawned types. 0 means ranges won't be modified.")]
		public int MatchesToModifySpawnedBlocksRange = 150;

		[Tooltip("Selects what should happen when score to modify is reached.")]
		public ModifyBlocksRangeType ModifyBlocksRangeType;


		[Tooltip("Read-only. Used for debug.")]
		public int RandomInitialLevelSeed;

		// NOTE: This is not the same object as in PlaythroughData. Serializing doesn't work (easily) with references.
		public System.Random Random = null;

		public float SpawnWildBlocksChance = 0f;
		public float SpawnBlockSmiteChance = 0f;
		public float SpawnRowSmiteChance = 0f;

		public GridRules Rules;

		// Rows from bottom to top go from 0 to n (up direction).
		public BlocksGrid Grid;

		// The valid size where blocks can be placed by the player. It should be smaller than the grid itself.
		public GridCoords PlayableSize;

		[SerializeReference]
		public ScoreGrid Score;

		[Tooltip("How much matches (according to the rules) does the player has to do to pass this level. 0 means it is an endless game.")]
		public int ObjectiveEndCount;

		// Endless games don't have end objective count. When player reaches the top, he wins, instead of loosing.
		public bool IsEndlessGame => ObjectiveEndCount == 0;

		public int ObjectiveRemainingCount => Mathf.Max(ObjectiveEndCount - Score.ObjectiveProgress, 0);

		public TowerLevelRunningState RunningState = TowerLevelRunningState.Running;
		public bool IsPlaying => RunningState == TowerLevelRunningState.Running;

		// Use Math.Floor() + 1, instead of Math.Ceiling() as initial value may be exactly 0 giving bad results.
		public GridCoords CalcFallShapeCoordsAt(int column) => new GridCoords(Grid.Rows - (int)Math.Floor(FallDistanceNormalized) - 1, column);
		public GridCoords FallShapeCoords => CalcFallShapeCoordsAt(FallingColumn);

		// Used for debug / cheat.
		[NonSerialized]
		public bool FallFrozen = false;

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (Rules.ObjectiveType == 0) {
				Debug.LogError($"{nameof(GridLevelData)} has no ObjectiveType set.", context);
			}

			if (BlocksSkinSet) {
				BlocksSkinSet.Validate(repo, context);
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(GridLevelData))]
	public class TowerLevelDataDrawer : SerializeReferenceCreatorDrawer<GridLevelData>
	{
	}
#endif
}