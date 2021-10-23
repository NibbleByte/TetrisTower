using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class ScoreDisplayUIController : MonoBehaviour, ILevelLoadListener
	{
		private TowerLevelController m_TowerLevel;

		private TowerLevelData m_LevelData => m_TowerLevel.LevelData;

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
			TotalScoreText.text = TotalScorePrefix + m_PlaythroughData.TotalScore;
			CurrentScoreText.text = CurrentScorePrefix + m_LevelData.Score.Score;
			RemainingText.text = RemainingPrefix + Mathf.Max(m_LevelData.ClearBlocksEndCount - m_LevelData.Score.TotalClearedBlocksCount, 0);
		}
	}

}