using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class MatchSequenceScoreUIController : MonoBehaviour
	{
		public Text MatchesCountText;
		public Text MatchedScoreText;

		private void Awake()
		{
			if (MatchesCountText) MatchesCountText.gameObject.SetActive(false);
			if (MatchedScoreText) MatchedScoreText.gameObject.SetActive(false);
		}

		public void UpdateScore(ScoreGrid scoreGrid)
		{
			if (MatchesCountText && scoreGrid.CurrentMatchesCount > 1) {
				MatchesCountText.gameObject.SetActive(true);
				MatchesCountText.text = "x" + scoreGrid.CurrentMatchesCount.ToString();
			}

			if (MatchedScoreText) {
				MatchedScoreText.gameObject.SetActive(true);
				MatchedScoreText.text = scoreGrid.CurrentSequenceScore.ToString();
			}
		}

		public void FinishScore(ScoreGrid scoreGrid)
		{
			Awake();
		}
	}
}