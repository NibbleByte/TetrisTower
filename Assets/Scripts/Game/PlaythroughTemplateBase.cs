using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[JsonObject(MemberSerialization.Fields)]
	public interface IPlaythroughData
	{
		// In case of multiplayer there can be multiple levels active at the same time.
		IReadOnlyList<GridLevelData> ActiveTowerLevels { get; }

		IEnumerable<PlaythroughPlayer> ActivePlayers { get; }

		public BlocksSkinSet BlocksSet { get; }

		public bool HaveFinishedLevels { get; }

		public float TotalPlayTime { get; }
		public int TotalScore { get; }

		ILevelSupervisor PrepareSupervisor();

		void AssignPlayer(PlaythroughPlayer player, GridLevelData levelData);
		void PausePlayers(IPlayerContext playerWithInputPreserved, object source);
		void ResumePlayers(object source);

		GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene);

		void RetryLevel();
		void QuitLevel();
		void FinishLevel();

		void SetupRandomGenerator(int seed = 0, bool resetCurrentLevelRandom = false);

		void ReplaceCurrentLevel(GridLevelData levelData);

		void Validate(Core.AssetsRepository repo, UnityEngine.Object context);
	}

	public abstract class PlaythroughTemplateBase : ScriptableObject
	{
		public abstract IPlaythroughData GeneratePlaythroughData(GameConfig config);

		public abstract IEnumerable<LevelParamData> GetAllLevels();

		public abstract bool HasActiveLevel { get; }
	}
}