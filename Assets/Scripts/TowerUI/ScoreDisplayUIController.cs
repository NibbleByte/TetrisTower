using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections.Generic;
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
		public string TotalScoreFormat = "Total: {VALUE}";
		public string CurrentScoreFormat = "Score: {VALUE} ({BONUS_RATIO})";
		public string BlocksClearedCountFormat = "Blocks: {VALUE}";
		public string BonusRatioFormat = "Bonus Ratio: {VALUE}";

		[Header("UI Texts")]
		public TextMeshProUGUI TotalScoreText;
		public TextMeshProUGUI CurrentScoreText;
		public TextMeshProUGUI BlocksClearedCountText;
		public TextMeshProUGUI BonusRatioText;

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
				TotalScoreText.text = TotalScoreFormat.Replace("{VALUE}", m_PlaythroughData.TotalScore.ToString());
			}

			// WorldMap doesn't have this.
			if (m_TowerLevel == null)
				return;

			if (CurrentScoreText) {
				CurrentScoreText.text = CurrentScoreFormat.Replace("{VALUE}", m_LevelData.Score.Score.ToString()).Replace("{BONUS_RATIO}", m_LevelData.Score.BonusRatio.ToString("0.000"));
			}

			if (BlocksClearedCountText) {
				BlocksClearedCountText.text = BlocksClearedCountFormat.Replace("{VALUE}", m_LevelData.Score.TotalClearedBlocksCount.ToString());
			}
			if (BonusRatioText) {
				BonusRatioText.text = BonusRatioFormat.Replace("{VALUE}", m_LevelData.Score.BonusRatio.ToString("0.000"));
			}
		}
	}

}