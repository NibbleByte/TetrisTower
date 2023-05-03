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
			public int Score;
			public int ClearedBlocks;
			public bool Completed;	// Player needs to complete the level objective once to unlock the adjacent levels.
		}

		public WorldLevelsSet LevelsSet;

		public List<WorldLevelAccomplishment> Accomplishments = new List<WorldLevelAccomplishment>();

		private string m_CurrentLevelID;

		public override bool IsFinalLevel => false;	// World doesn't have a final level... for now?!
		public override bool HaveFinishedLevels => Accomplishments.Where(a => a.Completed).Count() >= LevelsSet.Levels.Length;

		public override ILevelSupervisor PrepareSupervisor()
		{
			return TowerLevel == null ? new WorldMap.WorldMapLevelSupervisor(this) : new TowerLevelSupervisor(this);
		}

		public override void SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (string.IsNullOrWhiteSpace(m_CurrentLevelID) || !LevelsSet.GetLevel(m_CurrentLevelID).IsValid) {
				Debug.LogError($"Current level \"{m_CurrentLevelID}\" is invalid. Abort!");
				m_TowerLevel = null;
				return;
			}

			m_TowerLevel = GenerateTowerLevelData(gameConfig, LevelsSet.GetLevel(m_CurrentLevelID).LevelAsset.LevelParam);

			if (overrideScene != null) {
				TowerLevel.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {m_CurrentLevelID} - \"{m_TowerLevel.BackgroundScene?.ScenePath}\".");
		}

		public override void FinishLevel()
		{
			base.FinishLevel();

			WorldLevelAccomplishment accomplishment = Accomplishments.FirstOrDefault(a => a.LevelID == m_CurrentLevelID);
			if (accomplishment == null) {
				accomplishment = new WorldLevelAccomplishment() { LevelID = m_CurrentLevelID };

				Accomplishments.Add(accomplishment);
			}

			accomplishment.Score = m_TowerLevel.Score.Score;
			accomplishment.ClearedBlocks = m_TowerLevel.Score.TotalClearedBlocksCount;
			accomplishment.Completed = true; // TODO: this should be based on the objective.

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