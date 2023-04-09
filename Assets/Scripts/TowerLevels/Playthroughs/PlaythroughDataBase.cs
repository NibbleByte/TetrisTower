using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TetrisTower.TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Contains basic parameters for any play through. Inherits need to specify how levels are defined.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public abstract class PlaythroughDataBase : IPlaythroughData
	{
		[SerializeReference]
		[FormerlySerializedAs("TowerLevel")]
		protected GridLevelData m_TowerLevel;
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


		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + m_TowerLevel?.Score.Score ?? 0;

		public float PlayTimeOnLevelStart = 0f;
		public float TotalPlayTime => PlayTimeOnLevelStart + m_TowerLevel?.PlayTime ?? 0;

		public abstract bool IsFinalLevel { get; }
		public abstract bool HaveFinishedLevels { get; }

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

		public virtual void FinishLevel()
		{
			if (m_TowerLevel == null || m_TowerLevel.IsPlaying || !m_TowerLevel.HasWon)
				throw new InvalidOperationException($"Trying to finish level while it hasn't been won.");

			// NOTE: This may be done earlier to show the score to the user. In that case, this should do nothing.
			m_TowerLevel.Score.ConsumeBonusScore();

			ScoreOnLevelStart += m_TowerLevel.Score.Score;
			PlayTimeOnLevelStart += m_TowerLevel.PlayTime;

			m_TowerLevel = null;
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

		public GridLevelData GenerateTowerLevelData(GameConfig config, LevelParamData lp)
		{
			if (Random == null) {
				SetupRandomGenerator();
			}
			int seed = Random.Next();

			if (BlocksSet && config.DefaultBlocksSet.BlockSkins.Length != BlocksSet.BlockSkins.Length) {
				Debug.LogError($"Playthrough blocks count {BlocksSet.BlockSkins.Length} doesn't match the ones in the game config {config.DefaultBlocksSet.BlockSkins.Length}");
			}

			BlocksSkinStack skinsStack = new BlocksSkinStack(config.DefaultBlocksSet, BlocksSet);
			if (lp.SkinOverrides != null) {    // SO may not have filled this field yet...?
				skinsStack.AppendSkinSets(lp.SkinOverrides);
			}

			GridShapeTemplate[] shapeTemplates = lp.ShapeTemplates.Length != 0 ? lp.ShapeTemplates : config.ShapeTemplates;

			var pinnedBlocks = (lp.StartGridWithPinnedBlocks && lp.StartGrid != null)
				? lp.StartGrid.EnumerateOccupiedCoords()
				: Enumerable.Empty<GridCoords>()
				;

			// No, we don't need '+1'. 9 + 3 = 12 (0 - 11). Placing on the 9 takes (9, 10, 11).
			int extraRows = shapeTemplates.Max(st => st.Rows);

			// The shapes are spawned at the top of the grid (totalRows).
			// We need twice the shape size, to accommodate for shape placed ON TOP of the playable area
			// (e.g. 3 blocks flashing) + another shape on top for game over.
			int totalRows = lp.GridRows + extraRows * 2;

			var levelData = new GridLevelData() {
				BackgroundScene = lp.GetAppropriateBackgroundScene()?.Clone() ?? new SceneReference(),
				GreetMessage = lp.GreetMessage,

				ShapeTemplates = shapeTemplates.ToArray(),
				BlocksSkinStack = skinsStack,

				FallSpeedNormalized = FallSpeedNormalized,
				FallSpeedupPerAction = FallSpeedupPerAction,

				InitialSpawnBlockTypesCount = lp.InitialSpawnBlockTypesCount,
				SpawnBlockTypesRange = new Vector2Int(0, lp.InitialSpawnBlockTypesCount),
				MatchesToModifySpawnedBlocksRange = MatchesToModifySpawnedBlocksRange,
				ModifyBlocksRangeType = ModifyBlocksRangeType,

				RandomInitialLevelSeed = seed,
				Random = new Xoshiro.PRNG32.XoShiRo128starstar(seed),

				SpawnWildBlocksChance = lp.SpawnWildBlocks ? WildBlockChance : 0f,
				SpawnBlockSmiteChance = lp.SpawnBlockSmites ? BlockSmiteChance : 0f,
				SpawnRowSmiteChance = lp.SpawnRowSmites ? RowSmiteChance : 0f,

				Rules = lp.Rules,
				Grid = lp.StartGrid != null ? new BlocksGrid(lp.StartGrid, totalRows, lp.GridColumns, pinnedBlocks) : new BlocksGrid(totalRows, lp.GridColumns),
				PlayableSize = new GridCoords(lp.GridRows, lp.GridColumns),

				ObjectiveEndCount = lp.ObjectiveEndCount,
			};

			return levelData;
		}

		public virtual void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			if (m_TowerLevel != null) {
				m_TowerLevel.Validate(repo, context);
			}

			if (m_BlocksSet) {
				m_BlocksSet.Validate(repo, context);
			}
		}
	}
}