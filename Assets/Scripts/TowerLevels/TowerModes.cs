using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TetrisTower.Game;

namespace TetrisTower.TowerLevels.Modes
{
	public enum TowerDifficulty
	{
		Easy = -1,
		Medium = 0,
		Hard = 1,
	}

	[JsonObject(MemberSerialization.Fields)]
	public class TowerModesHighScoresDatabase
	{
		[JsonObject(MemberSerialization.Fields)]
		private class TowerHighScoreEntry
		{
			public PlaythroughTemplateBase Mode;
			public string LevelID;
			public TowerDifficulty Difficulty;
			public int Seed;

			public int HighScore;
		}

		public bool ModeStarted => m_CurrentEntry != null;
		public int StartedModeHighScore => m_CurrentEntry.HighScore;

		private List<TowerHighScoreEntry> m_Entries = new List<TowerHighScoreEntry>();

		[JsonIgnore]
		private TowerHighScoreEntry m_CurrentEntry;

		public int GetHighScoreForMode(PlaythroughTemplateBase mode, string levelID, TowerDifficulty difficulty, int seed)
		{
			var entry = FindEntry(mode, levelID, difficulty, seed);
			return entry != null ? entry.HighScore : 0;
		}

		public void StartTowerMode(PlaythroughTemplateBase mode, string levelID, TowerDifficulty difficulty, int seed)
		{
			if (m_CurrentEntry != null)
				throw new InvalidOperationException("Tower mode already started. Finish the last one first.");

			m_CurrentEntry = FindEntry(mode, levelID, difficulty, seed);
			if (m_CurrentEntry == null) {
				m_CurrentEntry = new TowerHighScoreEntry { Mode = mode, LevelID = levelID, Difficulty = difficulty, Seed = seed };

				m_Entries.Add(m_CurrentEntry);
			}
		}

		public bool CheckTowerModeScore(int score, GameConfig gameConfig)
		{
			if (m_CurrentEntry == null)
				throw new InvalidOperationException("Tower mode not started.");

			if (score > m_CurrentEntry.HighScore) {
				m_CurrentEntry.HighScore = score;

				Saves.SavesManager.SaveTowerModesHighScoresDatabase(this, gameConfig);

				return true;
			}

			return false;
		}

		public void ClearTowerMode()
		{
			m_CurrentEntry = null;
		}

		private TowerHighScoreEntry FindEntry(PlaythroughTemplateBase mode, string levelID, TowerDifficulty difficulty, int seed)
		{
			foreach (var entry in m_Entries) {
				if (entry.Mode == mode && entry.LevelID == levelID && entry.Difficulty == difficulty && entry.Seed == seed) {
					return entry;
				}
			}

			return null;
		}
	}

}