using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapDebugAPI : MonoBehaviour, ILevelLoadedListener
	{
		private WorldMapController m_WorldLevel;
		private WorldPlaythroughData m_PlaythroughData;

		public GameObject ProfilerStatsPrefab;
		private GameObject m_ProfilerStatsObject;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_WorldLevel);
			context.SetByType(out m_PlaythroughData);
		}

		public void OnLevelUnloading()
		{
		}

		public void ToggleDebug()
		{
			if (m_ProfilerStatsObject == null && ProfilerStatsPrefab) {
				m_ProfilerStatsObject = Instantiate(ProfilerStatsPrefab);
				return;
			}

			m_ProfilerStatsObject.SetActive(!m_ProfilerStatsObject.activeSelf);
		}

		public void UnlockLevels()
		{
			var accomplishments = new List<WorldPlaythroughData.WorldLevelAccomplishment>();

			foreach(var levelData in m_PlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>()) {
				var accomplishment = m_PlaythroughData.GetAccomplishment(levelData.LevelID);

				if (!accomplishment.IsValid) {
					accomplishment.LevelID = levelData.LevelID;
				}

				if (accomplishment.State < WorldLocationState.Unlocked) {
					accomplishment.State = WorldLocationState.Unlocked;
				}

				accomplishments.Add(accomplishment);
			}

			m_PlaythroughData.__ReplaceAccomplishments(accomplishments);
			m_PlaythroughData.DataIntegrityCheck();

			m_WorldLevel.StartCoroutine(m_WorldLevel.RevealReachedLocations());
		}

		public void CompleteLevel(string levelID)
		{
			var accomplishments = new List<WorldPlaythroughData.WorldLevelAccomplishment>();

			foreach (var levelData in m_PlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>()) {
				var accomplishment = m_PlaythroughData.GetAccomplishment(levelData.LevelID);

				if (!accomplishment.IsValid) {
					accomplishment.LevelID = levelData.LevelID;
				}

				if (levelID == levelData.LevelID) {
					accomplishment.State = WorldLocationState.Completed;
					accomplishment.HighestScore = levelData.ScoreToStars.LastOrDefault();
				}

				accomplishments.Add(accomplishment);
			}

			m_PlaythroughData.__ReplaceAccomplishments(accomplishments);
			m_PlaythroughData.DataIntegrityCheck();

			m_WorldLevel.RefreshLocation(levelID);
			m_WorldLevel.StartCoroutine(m_WorldLevel.RevealReachedLocations());
		}
	}
}