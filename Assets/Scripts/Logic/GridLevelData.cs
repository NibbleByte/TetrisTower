using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum TowerLevelRunningState
	{
		Preparing,
		Running,
		Finished,
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
		public float PrepareTime = 0;

		public GridShapeTemplate[] ShapeTemplates;

		public BlocksSkinStack BlocksSkinStack;
		public BlockSkin[] NormalBlocksSkins => BlocksSkinStack.GetNormalBlocks();

		// Initial spawn blocks from the array.
		public int InitialSpawnBlockTypesCount = 3;

		// Spawn blocks from the array in range [0, TypesCount).
		public Vector2Int SpawnBlockTypesRange = new Vector2Int(0, 3);

		[Tooltip("Read-only. Used also for VisualsRandom.")]
		public int RandomInitialLevelSeed;

		// NOTE: This is not the same object as in PlaythroughData. Serializing doesn't work (easily) with references.
		public System.Random Random = null;

		public GridRules Rules = new GridRules();

		// Rows from bottom to top go from 0 to n (up direction).
		public BlocksGrid Grid;

		// The valid size where blocks can be placed by the player. It should be smaller than the grid itself.
		public GridCoords PlayableSize;

		[SerializeReference]
		public ScoreGrid Score;

		[SerializeReference]
		public List<Objective> Objectives = new List<Objective>();

		public TowerLevelRunningState RunningState = TowerLevelRunningState.Preparing;
		public bool IsPlaying => RunningState <= TowerLevelRunningState.Running;
		public bool HasWon = false;

		public string GreetMessage = "";

		// Use Math.Floor() + 1, instead of Math.Ceiling() as initial value may be exactly 0 giving bad results.
		public GridCoords CalcFallShapeCoordsAt(int column) => new GridCoords(Grid.Rows - (int)Math.Floor(FallDistanceNormalized) - 1, column);
		public GridCoords FallShapeCoords => CalcFallShapeCoordsAt(FallingColumn);

		// Used for debug / cheat.
		[NonSerialized]
		public bool FallFrozen = false;

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (Objectives != null) {
				if (Objectives.Count == 0) {
					Debug.LogError($"{context} has no objectives set.", context);
				} else {
					foreach (Objective objective in Objectives.Where(obj => obj != null)) {
						objective.Validate(context);
					}
				}
			}

			if (BlocksSkinStack != null) {
				BlocksSkinStack.Validate(repo, context);
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