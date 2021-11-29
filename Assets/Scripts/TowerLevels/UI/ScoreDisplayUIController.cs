using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class ScoreDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		private GridLevelController m_TowerLevel;

		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private PlaythroughData m_PlaythroughData;

		[Header("Prefixes")]
		public string TotalScorePrefix = "Total: ";
		public string CurrentScorePrefix = "Current: ";
		public string RemainingPrefix = "Remaining: ";

		[Header("UI Texts")]
		public Text TotalScoreText;
		public Text CurrentScoreText;
		public Text RemainingText;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);
			contextReferences.SetByType(out m_PlaythroughData);

			m_TowerLevel.RunningActionsSequenceFinished += UpdateScore;

			UpdateScore();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.RunningActionsSequenceFinished -= UpdateScore;
		}

		private void UpdateScore()
		{
			if (TotalScoreText) {
				TotalScoreText.text = TotalScorePrefix + m_PlaythroughData.TotalScore;
			}

			if (CurrentScoreText) {
				CurrentScoreText.text = CurrentScorePrefix + m_LevelData.Score.Score;
			}

			if (RemainingText) {
				RemainingText.text = RemainingPrefix + m_LevelData.ClearBlocksRemainingCount;
			}
		}
	}

}