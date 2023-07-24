using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class ScoreDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		private GridLevelController m_TowerLevel;

		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private IPlaythroughData m_PlaythroughData;

		[Header("Prefixes")]
		public string TotalScorePrefix = "Total: ";
		public string CurrentScorePrefix = "Current: ";
		public string BlocksClearedCountPrefix = "Blocks: ";
		public string BonusRatioPrefix = "Bonus Ratio: ";

		[Header("UI Texts")]
		public TextMeshProUGUI TotalScoreText;
		public TextMeshProUGUI CurrentScoreText;
		public TextMeshProUGUI BlocksClearedCountText;
		public TextMeshProUGUI BonusRatioText;
		public TextMeshProUGUI ObjectivesText;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.TrySetByType(out m_TowerLevel);
			context.SetByType(out m_PlaythroughData);

			// WorldMap doesn't have this.
			if (m_TowerLevel) {
				m_TowerLevel.RunningActionsSequenceFinished += UpdateScore;
			}

			UpdateScore();
		}

		public void OnLevelUnloading()
		{
			if (m_TowerLevel) {
				m_TowerLevel.RunningActionsSequenceFinished -= UpdateScore;
			}
		}

		private void UpdateScore()
		{
			if (TotalScoreText) {
				TotalScoreText.text = TotalScorePrefix + m_PlaythroughData.TotalScore;
			}

			// WorldMap doesn't have this.
			if (m_TowerLevel == null)
				return;

			if (CurrentScoreText) {
				CurrentScoreText.text = CurrentScorePrefix + m_LevelData.Score.Score;
			}

			if (BlocksClearedCountText) {
				BlocksClearedCountText.text = BlocksClearedCountPrefix + m_LevelData.Score.TotalClearedBlocksCount;
			}
			if (BonusRatioText) {
				BonusRatioText.text = BonusRatioPrefix + m_LevelData.Score.BonusRatio.ToString("0.000");
			}

			if (ObjectivesText) {
				ObjectivesText.text = string.Join("\n", m_LevelData.Objectives
						.Select(obj => obj.GetDisplayText().Trim())
						.Where(t => !string.IsNullOrEmpty(t))
					);
			}
		}
	}

}