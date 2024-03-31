using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
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
		private WorldPlaythroughData m_PlaythroughData;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameConfig);
			context.SetByType(out m_LevelData);
			context.TrySetByType(out m_PlaythroughData);

			StarsPanelRoot.SetActive(m_PlaythroughData != null);
			HighScore?.gameObject.SetActive(m_PlaythroughData != null);
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
			if (m_PlaythroughData == null)
				return;

			var levelParam = m_PlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>().First(lp => lp.LevelID == m_PlaythroughData.CurrentLevelID);

			int earnedIndex = levelParam.CalculateStarsEarned(m_LevelData.Score.Score) - 1;
			for(int i = 0; i < StarImages.Length; i++) {
				StarImages[i].sprite = i <= earnedIndex ? m_GameConfig.StarEarnedSprite : m_GameConfig.StarMissingSprite;
				StarValues[i].text = levelParam.ScoreToStars[i].ToString();
			}

			if (HighScore) {
				HighScore.text = HighScoreFormat.Replace("{VALUE}", m_PlaythroughData.GetAccomplishment(levelParam.LevelID).HighestScore.ToString());
			}
		}
	}
}