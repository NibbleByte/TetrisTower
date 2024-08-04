using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Modes;
using TetrisTower.TowerLevels.Playthroughs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.WorldMap.UI
{
	public class WorldDisplayTowerStarsUIController : MonoBehaviour, ILevelLoadedListener
	{
		public GameObject StarsPanelRoot;
		public Image[] StarImages;
		public TMP_Text[] StarValues;

		public string HighScoreFormat = "High Score: {VALUE}";
		public TMP_Text HighScore;

		private GameConfig m_GameConfig;
		private GridLevelData m_LevelData;
		private IPlaythroughData m_PlaythroughData;

		private TowerModesHighScoresDatabase HighScoresDatabase => GameManager.Instance.GetManager<TowerModesHighScoresDatabase>();

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameConfig);
			context.SetByType(out m_LevelData);
			context.TrySetByType(out m_PlaythroughData);

			StarsPanelRoot.SetActive(m_PlaythroughData is WorldPlaythroughData);
			HighScore?.gameObject.SetActive(m_PlaythroughData is WorldPlaythroughData || HighScoresDatabase.ModeStarted);
		}

		public void OnLevelUnloading()
		{
		}

		void OnValidate()
		{
			if (StarImages != null && StarImages.Length != StarValues.Length) {
				Debug.LogError($"{this} has different number of images and values linked.", this);
			}
		}

		void OnEnable()
		{
			if (HighScoresDatabase.ModeStarted) {
				if (HighScore) {
					int highScore = HighScoresDatabase.StartedModeHighScore;
					int score = m_PlaythroughData.CanRetryLevel ? m_LevelData.Score.Score : m_PlaythroughData.TotalScore;
					if (score > highScore && m_LevelData.HasWon && (m_PlaythroughData.IsPlayingLastLevel || m_PlaythroughData.CanRetryLevel)) {
						highScore = score;

						HighScoresDatabase.CheckTowerModeScore(score, m_GameConfig);
						// TODO: Celebrate.
					}

					HighScore.text = HighScoreFormat.Replace("{VALUE}", highScore.ToString());
				}
				return;
			}

			WorldPlaythroughData worldPlaythroughData = m_PlaythroughData as WorldPlaythroughData;
			if (worldPlaythroughData == null)
				return;

			var levelParam = worldPlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>().First(lp => lp.LevelID == worldPlaythroughData.CurrentLevelID);

			int earnedIndex = levelParam.CalculateStarsEarned(m_LevelData.Score.Score) - 1;
			for(int i = 0; i < StarImages.Length; i++) {
				StarImages[i].sprite = i <= earnedIndex ? m_GameConfig.StarEarnedSprite : m_GameConfig.StarMissingSprite;
				StarValues[i].text = levelParam.ScoreToStars[i].ToString();
			}

			if (HighScore) {
				int highScore = worldPlaythroughData.GetAccomplishment(levelParam.LevelID).HighestScore;
				if (m_LevelData.Score.Score > highScore) {
					highScore = m_LevelData.Score.Score;
					// TODO: Celebrate.
				}

				HighScore.text = HighScoreFormat.Replace("{VALUE}", highScore.ToString());
			}
		}
	}
}