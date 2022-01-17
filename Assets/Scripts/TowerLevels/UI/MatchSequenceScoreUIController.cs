using System;
using System.Linq;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class MatchSequenceScoreUIController : MonoBehaviour, IMatchSequenceScoreDisplayer
	{
		public TextMeshProUGUI ClearCombosCountText;
		public TextMeshProUGUI ClearedBlocksCountText;

		private void Awake()
		{
			if (ClearCombosCountText) ClearCombosCountText.gameObject.SetActive(false);
			if (ClearedBlocksCountText) ClearedBlocksCountText.gameObject.SetActive(false);
		}

		public void UpdateScore(ScoreGrid scoreGrid)
		{
			if (ClearCombosCountText && scoreGrid.CurrentClearActionsCount > 1) {
				ClearCombosCountText.gameObject.SetActive(true);
				ClearCombosCountText.text = "x" + scoreGrid.CurrentClearActionsCount.ToString();
			}

			if (ClearedBlocksCountText) {
				ClearedBlocksCountText.gameObject.SetActive(true);
				ClearedBlocksCountText.text = "+" + scoreGrid.CurrentClearedBlocksCount.ToString();
			}
		}

		public void FinishScore(ScoreGrid scoreGrid)
		{
			Awake();
		}
	}
}