using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Contains basic parameters for any play through. Inherits need to specify how levels are defined.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public abstract class PlaythroughDataBase : IPlaythroughData
	{
		[SerializeReference]
		protected List<GridLevelData> m_ActiveTowerLevels = new List<GridLevelData>();
		public IReadOnlyList<GridLevelData> ActiveTowerLevels => m_ActiveTowerLevels;

		protected List<PlaythroughPlayer> m_Players = new List<PlaythroughPlayer>();
		public IEnumerable<PlaythroughPlayer> ActivePlayers => m_Players.Where(p => p != null);
		public bool IsSinglePlayer => ActivePlayers.Count() <= 1;
		public bool IsMultiPlayer => ActivePlayers.Count() > 1;



		[Tooltip("Blocks to be used. If empty, default set from the game config is used.")]
		[SerializeField]
		[FormerlySerializedAs("BlocksSet")]
		private BlocksSkinSet m_BlocksSet;
		public BlocksSkinSet BlocksSet => m_BlocksSet;

		[Tooltip("Leave the seed to 0 for random seed every time.")]
		public int RandomSeed = 0;
		public System.Random Random = null;

		[Tooltip("Read-only. Used for debug.")]
		public int RandomInitialSeed;


		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + (m_ActiveTowerLevels.FirstOrDefault()?.Score.Score ?? 0);

		public float PlayTimeOnLevelStart = 0f;
		public float TotalPlayTime => PlayTimeOnLevelStart + (m_ActiveTowerLevels.FirstOrDefault()?.PlayTime ?? 0);

		public virtual bool QuitLevelCanResumePlaythrough => false;
		public abstract bool HaveFinishedLevels { get; }

		public abstract ILevelSupervisor PrepareSupervisor();

		public virtual void AssignPlayer(PlaythroughPlayer player, GridLevelData levelData)
		{
			int index = m_ActiveTowerLevels.IndexOf(levelData);
			if (index == -1)
				throw new InvalidOperationException("Provided level data is not currently active.");

			while(m_Players.Count <= index) {
				m_Players.Add(null);
			}

			if (m_Players[index] != null) {
				throw new InvalidOperationException($"Trying to assign player {player} to level {levelData}, but another player {m_Players[index]} is already assigned to it.");
			}
			m_Players[index] = player;
		}

		public void PausePlayers(IPlayerContext playerWithInputPreserved, object source)
		{
			foreach (var player in m_Players) {
				player.Pause((IPlayerContext)player.PlayerContext != playerWithInputPreserved, source);
			}
		}

		public void ResumePlayers(object source)
		{
			foreach (var player in m_Players) {
				player.Resume(source);
			}
		}

		protected virtual void DisposeTowerLevels()
		{
			foreach(var player in m_Players) {
				player.Dispose();
			}

			m_Players.Clear();
			m_ActiveTowerLevels.Clear();
		}

		public abstract GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene);

		public void ReplaceCurrentLevel(GridLevelData levelData)
		{
			DisposeTowerLevels();

			Debug.Log($"Replacing current level with {levelData?.BackgroundScene?.ScenePath}");
			m_ActiveTowerLevels.Add(levelData);
		}

		public virtual void RetryLevel()
		{
			if (m_ActiveTowerLevels.Count == 0)
				throw new InvalidOperationException($"Trying to retry a level when no level is in progress.");

			DisposeTowerLevels();
		}

		public virtual void QuitLevel()
		{
			if (m_ActiveTowerLevels.Count == 0)
				throw new InvalidOperationException($"Trying to quit a level when no level is in progress.");

			DisposeTowerLevels();
		}

		public virtual void FinishLevel()
		{
			var winner = m_ActiveTowerLevels.FirstOrDefault(p => !p.IsPlaying && p.HasWon);
			if (winner == null)
				throw new InvalidOperationException($"Trying to finish level while it hasn't been won.");

			// NOTE: This may be done earlier to show the score to the user. In that case, this should do nothing.
			foreach(GridLevelData levelData in m_ActiveTowerLevels) {
				levelData.Score.ConsumeBonusScore();
			}

			ScoreOnLevelStart += winner.Score.Score;
			PlayTimeOnLevelStart += winner.PlayTime;

			DisposeTowerLevels();
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
				// All levels should start with the same seed.
				int levelSeed = Random.Next();

				foreach(GridLevelData levelData in m_ActiveTowerLevels) {
					levelData.RandomInitialLevelSeed = levelSeed;
					levelData.Random = new Xoshiro.PRNG32.XoShiRo128starstar(levelData.RandomInitialLevelSeed);
				}
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

				FallSpeedNormalized = lp.Rules.FallSpeedNormalized,
				FallSpeedupPerAction = lp.Rules.FallSpeedupPerAction,

				InitialSpawnBlockTypesCount = lp.InitialSpawnBlockTypesCount,
				SpawnBlockTypesRange = new Vector2Int(0, lp.InitialSpawnBlockTypesCount),

				RandomInitialLevelSeed = seed,
				Random = new Xoshiro.PRNG32.XoShiRo128starstar(seed),

				Rules = lp.Rules.Clone(),
				Grid = lp.StartGrid != null ? new BlocksGrid(lp.StartGrid, totalRows, lp.GridColumns, pinnedBlocks) : new BlocksGrid(totalRows, lp.GridColumns),
				PlayableSize = new GridCoords(lp.GridRows, lp.GridColumns),

				Objectives = lp.Objectives.Select(o => Saves.SavesManager.Clone<Objective>(o, config)).ToList(),
			};

			return levelData;
		}

		public virtual void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			foreach(GridLevelData levelData in m_ActiveTowerLevels) {
				if (levelData != null) {
					levelData.Validate(repo, context);
				}
			}

			if (m_BlocksSet) {
				m_BlocksSet.Validate(repo, context);
			}
		}
	}
}