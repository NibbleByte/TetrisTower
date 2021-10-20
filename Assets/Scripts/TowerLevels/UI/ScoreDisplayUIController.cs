using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class ScoreDisplayUIController : MonoBehaviour
	{
		public TowerLevelController TowerLevel;

		public TowerLevelData LevelData => TowerLevel.LevelData;

		[Header("Prefixes")]
		public string TotalScorePrefix = "Total: ";
		public string CurrentScorePrefix = "Current: ";
		public string RemainingPrefix = "Remaining: ";

		[Header("UI Texts")]
		public Text TotalScoreText;
		public Text CurrentScoreText;
		public Text RemainingText;

		void OnEnable()
		{
			TowerLevel.LevelInitialized += UpdateScore;
			TowerLevel.RunningActionsSequenceFinished += UpdateScore;

			// TODO: This is bad design.
			if (LevelData != null) {
				UpdateScore();
			}
		}

		void OnDisable()
		{
			if (TowerLevel) {
				TowerLevel.LevelInitialized -= UpdateScore;
				TowerLevel.RunningActionsSequenceFinished -= UpdateScore;
			}
		}

		private void UpdateScore()
		{
			TotalScoreText.text = TotalScorePrefix + 0;
			CurrentScoreText.text = CurrentScorePrefix + LevelData.Score.Score;
			RemainingText.text = RemainingPrefix + Mathf.Max(LevelData.ClearBlocksEndCount - LevelData.Score.TotalClearedBlocksCount, 0);
		}
	}

}