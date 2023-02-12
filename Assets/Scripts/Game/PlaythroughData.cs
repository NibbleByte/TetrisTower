using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TetrisTower.Game
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PlaythroughData
	{
		[SerializeReference]
		[FormerlySerializedAs("TowerLevel")]
		private GridLevelData m_TowerLevel;
		public GridLevelData TowerLevel => m_TowerLevel;



		[Tooltip("Blocks to be used. If empty, default set from the game config is used.")]
		[FormerlySerializedAs("BlocksSet")]
		private BlocksSkinSet m_BlocksSet;
		public BlocksSkinSet BlocksSet => m_BlocksSet;

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

		[Range(0f, 2f)]
		public float WildBlockChance = 0.15f;

		[Range(0f, 2f)]
		public float BlockSmiteChance = 0.15f;

		[Range(0f, 2f)]
		public float RowSmiteChance = 0.15f;


		public int CurrentLevelIndex = 0;
		public LevelParamData[] Levels;

		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + m_TowerLevel?.Score.Score ?? 0;

		public float PlayTimeOnLevelStart = 0f;
		public float TotalPlayTime => PlayTimeOnLevelStart + m_TowerLevel?.PlayTime ?? 0;

		public bool IsFinalLevel => CurrentLevelIndex == Levels.Length - 1;
		public bool HaveFinishedLevels => CurrentLevelIndex >= Levels.Length;

		public void SetupCurrentLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (CurrentLevelIndex >= Levels.Length) {
				Debug.LogError($"Current level {CurrentLevelIndex} is out of range {Levels.Length}. Abort!");
				m_TowerLevel = null;
				return;
			}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// Start the level with the same scene.
			if (overrideScene != null) {
				int foundIndex = Array.FindIndex(Levels, l => l.BackgroundScene.ScenePath == overrideScene.ScenePath);
				if (foundIndex != -1) {
					CurrentLevelIndex = foundIndex;
				}
			}
#endif

			m_TowerLevel = Levels[CurrentLevelIndex].GenerateTowerLevelData(gameConfig, this);

			if (overrideScene != null) {
				TowerLevel.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {CurrentLevelIndex} - \"{m_TowerLevel.BackgroundScene?.ScenePath}\".");
		}

		public void ReplaceCurrentLevel(GridLevelData levelData)
		{
			Debug.Log($"Replacing current level with {levelData?.BackgroundScene?.ScenePath}");
			m_TowerLevel = levelData;
		}

		public void RetryLevel()
		{
			if (m_TowerLevel == null)
				throw new InvalidOperationException($"Trying to retry a level when no level is in progress.");

			m_TowerLevel = null;
		}

		public void FinishLevel()
		{
			if (m_TowerLevel == null || m_TowerLevel.IsPlaying || !m_TowerLevel.HasWon)
				throw new InvalidOperationException($"Trying to finish level while it hasn't been won.");

			// NOTE: This may be done earlier to show the score to the user. In that case, this should do nothing.
			m_TowerLevel.Score.ConsumeBonusScore();

			ScoreOnLevelStart += m_TowerLevel.Score.Score;
			PlayTimeOnLevelStart += m_TowerLevel.PlayTime;

			m_TowerLevel = null;
			CurrentLevelIndex++;
		}

		public void SetupRandomGenerator(int seed = 0, bool resetCurrentLevelRandom = false)
		{
			if (Random != null) {
				throw new InvalidOperationException();
			}

			RandomSeed = seed != 0 ? seed : RandomSeed;
			RandomInitialSeed = RandomSeed != 0 ? RandomSeed : UnityEngine.Random.Range(0, int.MaxValue);
			Random = new Xoshiro.PRNG32.XoShiRo128starstar(RandomInitialSeed);

			if (resetCurrentLevelRandom) {
				TowerLevel.RandomInitialLevelSeed = Random.Next();
				TowerLevel.Random = new Xoshiro.PRNG32.XoShiRo128starstar(TowerLevel.RandomInitialLevelSeed);
			}
		}

		public void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (m_TowerLevel != null) {
				m_TowerLevel.Validate(repo, context);
			}

			if (m_BlocksSet) {
				m_BlocksSet.Validate(repo, context);
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

		[Multiline]
		public string GreetMessage = "";

		// Spawn blocks from the array in range [0, TypesCount).
		public int InitialSpawnBlockTypesCount = 3;

		[Tooltip("How much matches (according to the rules) does the player has to do to pass this level. 0 means it is an endless game.")]
		public int ObjectiveEndCount;

		[Tooltip("If true WildBlocks will be spawned with chance configured above. They match with any other type of block.")]
		public bool SpawnWildBlocks = false;

		[Tooltip("If true BlockSmite blocks will be spawned with chance configured above. They clear every block of the type it lands on.")]
		public bool SpawnBlockSmites = false;

		[Tooltip("If true RowSmite blocks will be spawned with chance configured above. They clear the row it lands on.")]
		public bool SpawnRowSmites = false;

		public GridRules Rules;

		[Range(5, 20)]
		public int GridRows = 9;

		// Validation value - cone visuals require different visual models for each size.
		// We don't support any other values for now.
		public const int SupportedColumnsCount = 13;
		[Range(SupportedColumnsCount, SupportedColumnsCount)]
		public int GridColumns = SupportedColumnsCount;

		public GridShapeTemplate[] ShapeTemplates;


		public bool StartGridWithPinnedBlocks = false;

		[SerializeReference]
		public BlocksGrid StartGrid;

		public BlocksSkinSet[] SkinOverrides;

		public SceneReference GetAppropriateBackgroundScene()
		{
			if (Platforms.PlatformsUtils.IsMobile) {
				return BackgroundSceneMobile.IsEmpty ? BackgroundScene : BackgroundSceneMobile;
			} else {
				return BackgroundScene;
			}
		}

		public GridLevelData GenerateTowerLevelData(GameConfig config, PlaythroughData playthrough)
		{
			if (playthrough.Random == null) {
				playthrough.SetupRandomGenerator();
			}
			int seed = playthrough.Random.Next();

			if (playthrough.BlocksSet && config.DefaultBlocksSet.BlockSkins.Length != playthrough.BlocksSet.BlockSkins.Length) {
				Debug.LogError($"Playthrough blocks count {playthrough.BlocksSet.BlockSkins.Length} doesn't match the ones in the game config {config.DefaultBlocksSet.BlockSkins.Length}");
			}

			BlocksSkinStack skinsStack = new BlocksSkinStack(config.DefaultBlocksSet, playthrough.BlocksSet);
			if (SkinOverrides != null) {	// SO may not have filled this field yet...?
				skinsStack.AppendSkinSets(SkinOverrides);
			}

			GridShapeTemplate[] shapeTemplates = ShapeTemplates.Length != 0 ? ShapeTemplates : config.ShapeTemplates;

			var pinnedBlocks = (StartGridWithPinnedBlocks && StartGrid != null)
				? StartGrid.EnumerateOccupiedCoords()
				: Enumerable.Empty<GridCoords>()
				;

			// No, we don't need '+1'. 9 + 3 = 12 (0 - 11). Placing on the 9 takes (9, 10, 11).
			int extraRows = shapeTemplates.Max(st => st.Rows);

			// The shapes are spawned at the top of the grid (totalRows).
			// We need twice the shape size, to accommodate for shape placed ON TOP of the playable area
			// (e.g. 3 blocks flashing) + another shape on top for game over.
			int totalRows = GridRows + extraRows * 2;

			var levelData = new GridLevelData() {
				BackgroundScene = GetAppropriateBackgroundScene()?.Clone() ?? new SceneReference(),
				GreetMessage = GreetMessage,

				ShapeTemplates = shapeTemplates.ToArray(),
				BlocksSkinStack = skinsStack,

				FallSpeedNormalized = playthrough.FallSpeedNormalized,
				FallSpeedupPerAction = playthrough.FallSpeedupPerAction,

				InitialSpawnBlockTypesCount = InitialSpawnBlockTypesCount,
				SpawnBlockTypesRange = new Vector2Int(0, InitialSpawnBlockTypesCount),
				MatchesToModifySpawnedBlocksRange = playthrough.MatchesToModifySpawnedBlocksRange,
				ModifyBlocksRangeType = playthrough.ModifyBlocksRangeType,

				RandomInitialLevelSeed = seed,
				Random = new Xoshiro.PRNG32.XoShiRo128starstar(seed),

				SpawnWildBlocksChance = SpawnWildBlocks ? playthrough.WildBlockChance : 0f,
				SpawnBlockSmiteChance = SpawnBlockSmites ? playthrough.BlockSmiteChance : 0f,
				SpawnRowSmiteChance = SpawnRowSmites ? playthrough.RowSmiteChance : 0f,

				Rules = Rules,
				Grid = StartGrid != null ? new BlocksGrid(StartGrid, totalRows, GridColumns, pinnedBlocks) : new BlocksGrid(totalRows, GridColumns),
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

#if UNITY_EDITOR
			if (GridRows == 0 || GridColumns == 0) {
				GridRows = 9;
				GridColumns = SupportedColumnsCount;
				UnityEditor.EditorUtility.SetDirty(context);
			}

			if (StartGrid != null && StartGrid.Rows == 0) {
				StartGrid = new BlocksGrid(GridRows, GridColumns);
				UnityEditor.EditorUtility.SetDirty(context);
			}
#endif

			if (GridRows == 0 || GridColumns == 0) {
				Debug.LogError($"Level param {BackgroundScene} has invalid grid size set.", context);
			}

			if (StartGrid != null && (StartGrid.Rows != GridRows || StartGrid.Columns != GridColumns)) {
				Debug.LogError($"Level param {BackgroundScene} has starting grid with wrong size.", context);
			}

			if (SkinOverrides != null && SkinOverrides.Any(s => !repo.IsRegistered(s))) {
				Debug.LogError($"Level param {nameof(SkinOverrides)} has sets that are not registered as serializable.", context);
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