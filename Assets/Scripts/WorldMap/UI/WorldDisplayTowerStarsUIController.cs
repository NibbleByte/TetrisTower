using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.WorldMap.UI
{
	public class WorldDisplayTowerStarsUIController : MonoBehaviour, ILevelLoadedListener
	{
		public Image[] StarImages;

		private GameConfig m_GameConfig;
		private GridLevelData m_LevelData;
		private WorldPlaythroughData m_PlaythroughData;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameConfig);
			context.SetByType(out m_LevelData);
			context.TrySetByType(out m_PlaythroughData);

			foreach (Image starImage in StarImages) {
				starImage.gameObject.SetActive(m_PlaythroughData != null);
			}
		}

		public void OnLevelUnloading()
		{
		}

		void OnEnable()
		{
			if (m_PlaythroughData == null)
				return;

			var levelParam = m_PlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>().First(lp => lp.LevelID == m_PlaythroughData.CurrentLevelID);

			int earnedIndex = levelParam.CalculateStarsEarned(m_LevelData.Score.Score) - 1;
			for(int i = 0; i < StarImages.Length; i++) {
				StarImages[i].sprite = i <= earnedIndex ? m_GameConfig.StarEarnedSprite : m_GameConfig.StarMissingSprite;
			}
		}
	}
}