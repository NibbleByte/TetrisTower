using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	public enum WorldLocationState
	{
		Hidden,
		Unlocked,	// Unlocked but not yet revealed to the player. UI has to do this.
		Revealed,	// Level is unlocked and shown to player.
		Completed   // Player needs to complete the level objective once to unlock the adjacent levels.
	}

	public static class WorldLocationExtensions
	{
		public static bool IsVisible(this WorldLocationState state) => state == WorldLocationState.Revealed || state == WorldLocationState.Completed;
	}

	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class WorldPlaythroughData : PlaythroughDataBase
	{
		[Serializable]
		public struct WorldLevelAccomplishment
		{
			public string LevelID;
			public int HighestScore;
			public float BestBonusRatio;
			public int MostClearedBlocks;
			public WorldLocationState State;

			public bool IsValid => !string.IsNullOrEmpty(LevelID);
		}

		// Keep it private - modding can add more levels, outside this set.
		[SerializeField]
		private WorldLevelsSet m_LevelsSet;

		public IReadOnlyCollection<WorldLevelAccomplishment> Accomplishments => m_Accomplishments;
		private WorldLevelAccomplishment[] m_Accomplishments = new WorldLevelAccomplishment[0];

		private string m_CurrentLevelID;

		public override bool QuitLevelCanResumePlaythrough => true;
		public override bool HaveFinishedLevels => m_Accomplishments.Where(a => a.State == WorldLocationState.Completed).Count() >= m_LevelsSet.LevelsCount;

		public WorldLevelAccomplishment GetAccomplishment(string levelID) => m_Accomplishments.FirstOrDefault(a => a.LevelID == levelID);

		public WorldLocationState GetLocationState(string levelID) => GetAccomplishment(levelID).State;

		public IEnumerable<LevelParamData> GetAllLevels() => m_LevelsSet.Levels;

		public IEnumerable<WorldLevelsLink> GetAllLevelLinks() => m_LevelsSet.LevelsLinks;

		public IEnumerable<string> GetLinkedLevelIDsFor(string levelID) => m_LevelsSet.GetLinkedLevelIDsFor(levelID);

		private int GetAccomplishmentIndex(string levelID, bool createIfMissing) {
			int index = Array.FindIndex(m_Accomplishments, a => a.LevelID == levelID);

			if (index == -1 && createIfMissing) {
				index = m_Accomplishments.Length;

				Array.Resize(ref m_Accomplishments, m_Accomplishments.Length + 1);

				m_Accomplishments[index] = new WorldLevelAccomplishment() { LevelID = levelID };
			}

			return index;
		}

		// For debug!
		internal void __ReplaceAccomplishments(IEnumerable<WorldLevelAccomplishment> accomplishments)
		{
			m_Accomplishments = accomplishments.ToArray();
		}

		public override ILevelSupervisor PrepareSupervisor()
		{
			return ActiveTowerLevels.Count == 0 && string.IsNullOrEmpty(m_CurrentLevelID) ? new WorldMap.WorldMapLevelSupervisor(this) : new TowerLevelSupervisor(this);
		}

		/// <summary>
		/// Call this to ensure location state is up to date (supporting save migration & modding).
		/// Checks if all locations linked to completed ones are unlocked.
		/// </summary>
		public void DataIntegrityCheck()
		{
			int startIndex = GetAccomplishmentIndex(m_LevelsSet.StartLevel.LevelParam.LevelID, true);
			ref WorldLevelAccomplishment startAccomplishment = ref m_Accomplishments[startIndex];
			if (startAccomplishment.State == WorldLocationState.Hidden) {
				startAccomplishment.State = WorldLocationState.Unlocked;
			}

			foreach (WorldMapLevelParamData level in GetAllLevels()) {
				if (GetAccomplishment(level.LevelID).State == WorldLocationState.Completed) {
					UnlockLinkedLevels(level.LevelID);
				}
			}
		}

		public void SetCurrentLevel(string levelID)
		{
			if (!string.IsNullOrEmpty(m_CurrentLevelID)) {
				Debug.LogError($"Setting current levelID to {levelID}, while another one is active - {m_CurrentLevelID}");
			}

			m_CurrentLevelID = levelID;
			Debug.Log($"Setting current level to {m_CurrentLevelID}");
		}

		public void TryCancelCurrentLevel()
		{
			if (!string.IsNullOrEmpty(m_CurrentLevelID)) {
				Debug.Log($"Cancelling current level {m_CurrentLevelID}");
			}

			if (m_ActiveTowerLevels.Count == 0) {
				m_CurrentLevelID = "";
			} else {
				QuitLevel();
			}
		}

		public override GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (string.IsNullOrWhiteSpace(m_CurrentLevelID) || m_LevelsSet.GetLevelData(m_CurrentLevelID) == null) {
				Debug.LogError($"Current level \"{m_CurrentLevelID}\" is invalid. Abort!");
				m_ActiveTowerLevels.Clear();
				return null;
			}

			m_ActiveTowerLevels.Clear();
			m_ActiveTowerLevels.Add(GenerateTowerLevelData(gameConfig, m_LevelsSet.GetLevelData(m_CurrentLevelID)));

			if (overrideScene != null) {
				m_ActiveTowerLevels[0].BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {m_CurrentLevelID} - \"{m_ActiveTowerLevels[0].BackgroundScene?.ScenePath}\".");

			return m_ActiveTowerLevels[0];
		}

		public override void QuitLevel()
		{
			base.QuitLevel();

			m_CurrentLevelID = "";
		}

		public override void FinishLevel()
		{
			int index = GetAccomplishmentIndex(m_CurrentLevelID, createIfMissing: true);

			ref WorldLevelAccomplishment accomplishment = ref m_Accomplishments[index];

			accomplishment.HighestScore = Mathf.Max(accomplishment.HighestScore, m_ActiveTowerLevels[0].Score.Score);
			accomplishment.MostClearedBlocks = Mathf.Max(accomplishment.MostClearedBlocks, m_ActiveTowerLevels[0].Score.TotalClearedBlocksCount);
			accomplishment.BestBonusRatio = Mathf.Max(accomplishment.BestBonusRatio, m_ActiveTowerLevels[0].Score.BonusRatio);

			accomplishment.State = WorldLocationState.Completed; // TODO: this should be based on the objective.

			base.FinishLevel();

			ScoreOnLevelStart = m_Accomplishments.Sum(a => a.HighestScore);

			UnlockLinkedLevels(m_CurrentLevelID);

			m_CurrentLevelID = "";
		}

		private void UnlockLinkedLevels(string levelID)
		{
			foreach (string linkedLevelID in GetLinkedLevelIDsFor(levelID)) {
				int linkedIndex = GetAccomplishmentIndex(linkedLevelID, true);

				ref WorldLevelAccomplishment linkedAccomplishment = ref m_Accomplishments[linkedIndex];

				if (linkedAccomplishment.State == WorldLocationState.Hidden) {
					linkedAccomplishment.State = WorldLocationState.Unlocked;
				}
			}
		}

		public void RevealUnlockedLevel(string levelID)
		{
			int index = GetAccomplishmentIndex(levelID, false);
			if (index == -1) {
				Debug.LogError($"{levelID} trying to reveal level that is not unlocked or missing.");
				return;
			}

			ref WorldLevelAccomplishment accomplishment = ref m_Accomplishments[index];

			if (accomplishment.State == WorldLocationState.Unlocked) {
				accomplishment.State = WorldLocationState.Revealed;
			} else {
				Debug.LogError($"Trying to reveal level {levelID} that is not unlocked. Found state: {accomplishment.State}.");
			}
		}

		public override void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			base.Validate(repo, context);

			m_LevelsSet.Validate(repo);
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(WorldPlaythroughData))]
	public class WorldPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<WorldPlaythroughData>
	{
	}
#endif
}