using DevLocker.GFrame;
using System;
using System.Linq;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerLevels.UI
{
	public class MatchSequenceScoreUIController : MonoBehaviour, ILevelLoadedListener
	{
		public TextMeshProUGUI ClearCombosCountText;
		public TextMeshProUGUI ClearedBlocksCountText;

		private ConeVisualsGrid m_VisualsGrid;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_VisualsGrid);

			m_VisualsGrid.ScoreUpdated += OnUpdateScore;
			m_VisualsGrid.ScoreFinished += OnFinishScore;

		}

		public void OnLevelUnloading()
		{
			m_VisualsGrid.ScoreUpdated -= OnUpdateScore;
			m_VisualsGrid.ScoreFinished -= OnFinishScore;

			m_VisualsGrid = null;
		}

		private void Awake()
		{
			if (ClearCombosCountText) ClearCombosCountText.gameObject.SetActive(false);
			if (ClearedBlocksCountText) ClearedBlocksCountText.gameObject.SetActive(false);
		}

		public void OnUpdateScore(ScoreGrid scoreGrid)
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

		public void OnFinishScore(ScoreGrid scoreGrid)
		{
			Awake();
		}
	}
}