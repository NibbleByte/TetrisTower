using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.Serialization;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class WorldPlaythroughData : PlaythroughDataBase
	{
		[Serializable]
		public class WorldLevelAccomplishment
		{
			public string LevelID;
			public int HighestScore = 0;
			public float BestBonusRatio = 1f;
			public int MostClearedBlocks = 0;
			public bool Completed;	// Player needs to complete the level objective once to unlock the adjacent levels.
		}

		public WorldLevelsSet LevelsSet;

		public List<WorldLevelAccomplishment> Accomplishments = new List<WorldLevelAccomplishment>();

		private string m_CurrentLevelID;

		public override bool IsFinalLevel => false;	// World doesn't have a final level... for now?!
		public override bool HaveFinishedLevels => Accomplishments.Where(a => a.Completed).Count() >= LevelsSet.Levels.Length;

		public WorldLevelAccomplishment GetAccomplishment(string levelID) => Accomplishments.FirstOrDefault(a => a.LevelID == levelID);

		public override ILevelSupervisor PrepareSupervisor()
		{
			return TowerLevel == null && string.IsNullOrEmpty(m_CurrentLevelID) ? new WorldMap.WorldMapLevelSupervisor(this) : new TowerLevelSupervisor(this);
		}

		public void SetCurrentLevel(string levelID)
		{
			if (!string.IsNullOrEmpty(m_CurrentLevelID)) {
				Debug.LogError($"Setting current levelID to {levelID}, while another one is active - {m_CurrentLevelID}");
			}

			m_CurrentLevelID = levelID;
			Debug.Log($"Setting current level to {m_CurrentLevelID}");
		}

		public override void SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (string.IsNullOrWhiteSpace(m_CurrentLevelID) || LevelsSet.GetLevel(m_CurrentLevelID) == null) {
				Debug.LogError($"Current level \"{m_CurrentLevelID}\" is invalid. Abort!");
				m_TowerLevel = null;
				return;
			}

			m_TowerLevel = GenerateTowerLevelData(gameConfig, LevelsSet.GetLevel(m_CurrentLevelID).LevelParam);

			if (overrideScene != null) {
				TowerLevel.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {m_CurrentLevelID} - \"{m_TowerLevel.BackgroundScene?.ScenePath}\".");
		}

		public override void FinishLevel()
		{
			WorldLevelAccomplishment accomplishment = GetAccomplishment(m_CurrentLevelID);
			if (accomplishment == null) {
				accomplishment = new WorldLevelAccomplishment() { LevelID = m_CurrentLevelID };

				Accomplishments.Add(accomplishment);
			}

			accomplishment.HighestScore = Mathf.Max(accomplishment.HighestScore, m_TowerLevel.Score.Score);
			accomplishment.MostClearedBlocks = Mathf.Max(accomplishment.MostClearedBlocks, m_TowerLevel.Score.TotalClearedBlocksCount);
			accomplishment.BestBonusRatio = Mathf.Max(accomplishment.BestBonusRatio, m_TowerLevel.Score.BonusRatio);

			accomplishment.Completed = true; // TODO: this should be based on the objective.

			base.FinishLevel();

			ScoreOnLevelStart = Accomplishments.Sum(a => a.HighestScore);

			m_CurrentLevelID = "";
		}

		public override void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			base.Validate(repo, context);

			LevelsSet.Validate(repo);
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(WorldPlaythroughData))]
	public class WorldPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<WorldPlaythroughData>
	{
	}
#endif
}