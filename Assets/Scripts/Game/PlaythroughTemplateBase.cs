using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[JsonObject(MemberSerialization.Fields)]
	public interface IPlaythroughData
	{
		GridLevelData TowerLevel { get; }

		public bool HaveFinishedLevels { get; }

		public float TotalPlayTime { get; }
		public int TotalScore { get; }

		void RetryLevel();
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