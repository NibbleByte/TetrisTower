using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
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
		public string RemainingPrefix = "Remaining: ";

		[Header("UI Texts")]
		public TextMeshProUGUI TotalScoreText;
		public TextMeshProUGUI CurrentScoreText;
		public TextMeshProUGUI BlocksClearedCountText;
		public TextMeshProUGUI BonusRatioText;
		public TextMeshProUGUI RemainingText;
		public TextMeshProUGUI RulesText;

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

			if (RemainingText) {
				RemainingText.text = RemainingPrefix + (m_LevelData.IsEndlessGame ? "Endless" : m_LevelData.ObjectiveRemainingCount.ToString());
			}

			if (RulesText) {
				RulesText.text = "";

				if (!m_LevelData.Rules.IsObjectiveAllMatchTypes) {
					if (m_LevelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Horizontal)) {
						RulesText.text += "Horizontal ";
					}
					if (m_LevelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Vertical)) {
						RulesText.text += "Vertical ";
					}
					if (m_LevelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Diagonals)) {
						RulesText.text += "Diagonals ";
					}

					RulesText.text += "Only!";
				}
			}
		}
	}

}