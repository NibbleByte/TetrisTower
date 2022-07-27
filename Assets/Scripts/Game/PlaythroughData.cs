using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PlaythroughData
	{
		[SerializeReference] public GridLevelData TowerLevel;

		[Tooltip("Blocks to be used. If empty, default set from the game config is used.")]
		public BlocksSet BlocksSet;

		public float FallSpeedNormalized = 2f;
		public float FallSpeedupPerAction = 0.01f;

		[Tooltip("Every x matches done by the player, increase the range of the spawned types. 0 means ranges won't be modified.")]
		public int MatchesToModifySpawnedBlocksRange = 150;

		[Tooltip("Selects what should happen when score to modify is reached.")]
		public ModifyBlocksRangeType ModifyBlocksRangeType;

		[Tooltip("Leave the seed to 0 for random seed every time.")]
		public int RandomSeed = 0;
		public System.Random Random = null;

		[Tooltip("Read-only. Used for debug.")]
		public int RandomInitialSeed;

		public int CurrentLevelIndex = 0;
		public LevelParamData[] Levels;

		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + TowerLevel?.Score.Score ?? 0;

		public float PlayTimeOnLevelStart = 0f;
		public float TotalPlayTime => PlayTimeOnLevelStart + TowerLevel?.PlayTime ?? 0;

		public bool IsFinalLevel => CurrentLevelIndex == Levels.Length - 1;
		public bool HaveFinishedLevels => CurrentLevelIndex >= Levels.Length;

		public void RetryLevel()
		{
			if (TowerLevel == null)
				throw new InvalidOperationException($"Trying to retry a level when no level is in progress.");

			TowerLevel = null;
		}

		public void FinishLevel()
		{
			if (TowerLevel == null || TowerLevel.RunningState != TowerLevelRunningState.Won)
				throw new InvalidOperationException($"Trying to finish level when it hasn't finished - {TowerLevel?.RunningState}.");

			// NOTE: This may be done earlier to show the score to the user. In that case, this should do nothing.
			TowerLevel.Score.ConsumeBonusScore();

			ScoreOnLevelStart += TowerLevel.Score.Score;
			PlayTimeOnLevelStart += TowerLevel.PlayTime;

			TowerLevel = null;
			CurrentLevelIndex++;
		}

		public void CreateRandomGenerator()
		{
			if (Random != null) {
				throw new InvalidOperationException();
			}

			RandomInitialSeed = RandomSeed != 0 ? RandomSeed : UnityEngine.Random.Range(0, int.MaxValue);
			Random = new Xoshiro.PRNG32.XoShiRo128starstar(RandomInitialSeed);
		}

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (TowerLevel != null) {
				TowerLevel.Validate(repo, context);
			}

			if (BlocksSet) {
				BlocksSet.Validate(repo, context);
			}

			foreach (var level in Levels) {
				level.Validate(repo, context);
			}
		}
	}

	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class LevelParamData
	{
		public SceneReference BackgroundScene;

		public SceneReference BackgroundSceneMobile;

		// Spawn blocks from the array in range [0, TypesCount).
		public int InitialSpawnBlockTypesCount = 3;

		[Tooltip("How much matches (according to the rules) does the player has to do to pass this level. 0 means it is an endless game.")]
		public int ObjectiveEndCount;

		public GridRules Rules;

		[Range(5, 20)]
		public int GridRows = 9;

		// Validation value - cone visuals require different visual models for each size.
		// We don't support any other values for now.
		public const int SupportedColumnsCount = 13;
		[Range(SupportedColumnsCount, SupportedColumnsCount)]
		public int GridColumns = SupportedColumnsCount;

		public GridShapeTemplate[] ShapeTemplates;

		public SceneReference GetAppropriateBackgroundScene()
		{
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			return BackgroundSceneMobile.IsEmpty ? BackgroundScene : BackgroundSceneMobile;
#else
			return BackgroundScene;
#endif
		}

		public GridLevelData GenerateTowerLevelData(GameConfig config, PlaythroughData playthrough)
		{
			if (playthrough.Random == null) {
				playthrough.CreateRandomGenerator();
			}
			int seed = playthrough.Random.Next();

			if (playthrough.BlocksSet && config.DefaultBlocksSet.Blocks.Length != playthrough.BlocksSet.Blocks.Length) {
				Debug.LogError($"Playthrough blocks count {playthrough.BlocksSet.Blocks.Length} doesn't match the ones in the game config {config.DefaultBlocksSet.Blocks.Length}");
			}
			BlocksSet blocks = playthrough.BlocksSet ?? config.DefaultBlocksSet;
			GridShapeTemplate[] shapeTemplates = ShapeTemplates.Length != 0	? ShapeTemplates : config.ShapeTemplates;

			// No, we don't need '+1'. 9 + 3 = 12 (0 - 11). Placing on the 9 takes (9, 10, 11).
			int extraRows = shapeTemplates.Max(st => st.Rows);

			// The shapes are spawned at the top of the grid (totalRows).
			// We need twice the shape size, to accommodate for shape placed ON TOP of the playable area
			// (e.g. 3 blocks flashing) + another shape on top for game over.
			int totalRows = GridRows + extraRows * 2;

			var levelData = new GridLevelData() {
				BackgroundScene = GetAppropriateBackgroundScene()?.Clone() ?? new SceneReference(),

				ShapeTemplates = shapeTemplates.ToArray(),
				BlocksSet = blocks,

				FallSpeedNormalized = playthrough.FallSpeedNormalized,
				FallSpeedupPerAction = playthrough.FallSpeedupPerAction,

				InitialSpawnBlockTypesCount = InitialSpawnBlockTypesCount,
				SpawnBlockTypesRange = new Vector2Int(0, InitialSpawnBlockTypesCount),
				MatchesToModifySpawnedBlocksRange = playthrough.MatchesToModifySpawnedBlocksRange,
				ModifyBlocksRangeType = playthrough.ModifyBlocksRangeType,

				RandomInitialLevelSeed = seed,
				Random = new Xoshiro.PRNG32.XoShiRo128starstar(seed),

				Rules = Rules,
				Grid = new BlocksGrid(totalRows, GridColumns),
				PlayableSize = new GridCoords(GridRows, GridColumns),

				ObjectiveEndCount = ObjectiveEndCount,
			};

			return levelData;
		}

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (Rules.ObjectiveType == 0) {
				Debug.LogError($"{nameof(PlaythroughData)} has no ObjectiveType set.", context);
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(PlaythroughData))]
	public class PlaythroughDataDrawer : SerializeReferenceCreatorDrawer<PlaythroughData>
	{
	}
#endif
}