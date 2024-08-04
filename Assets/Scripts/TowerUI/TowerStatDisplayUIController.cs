using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using TetrisTower.Game;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class TowerStatDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		public enum StatType
		{
			Score,
			BonusRatio,
			BlocksCleared,
			PlayTime,
		}

		public enum StatSourceType
		{
			Current,
			Total,
			TotalIfNoRetry,
		}

		private GridLevelController m_TowerLevel;

		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private IPlaythroughData m_PlaythroughData;

		public StatType Stat;
		public StatSourceType Source;


		[Tooltip("Format the stat text. Insert stat with {VALUE} and {BONUS_RATIO}\n\nFor play time use the C# TimeStamp formatting. Example:\n'Play time: 'mm\\:ss\\.fff\nUse '' for arbitrary text. Use {totalSeconds} to display time in seconds (no minutes).")]
		public string DisplayFormat = "{VALUE}";

		private TextMeshProUGUI m_DisplayText;

		private bool m_UseCurrentSource => Source switch {
				StatSourceType.Current => true,
				StatSourceType.Total => false,
				// Sprint & Marathon (sequence playthrough) prefers the Total source.
				StatSourceType.TotalIfNoRetry => m_PlaythroughData.CanRetryLevel,
				_ => throw new NotImplementedException(),
			};

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.TrySetByType(out m_TowerLevel);
			context.SetByType(out m_PlaythroughData);

			m_DisplayText = GetComponent<TextMeshProUGUI>();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel = null;
			m_PlaythroughData = null;
		}

		void OnValidate()
		{
			switch(Stat) {
				case StatType.BlocksCleared:
					if (Source != StatSourceType.Current) {
						Debug.LogError($"Stat type {Stat} supports only Current source for {name}.", this);
					}
					break;
			}
		}

		void Update()
		{
			if (m_PlaythroughData == null)
				return;

			switch (Stat) {
				case StatType.Score: DisplayScore(); break;
				case StatType.PlayTime: DisplayPlayTime(); break;
				case StatType.BonusRatio: DisplayBonusRatio(); break;
				case StatType.BlocksCleared: DisplayBlocksCleared(); break;
			}
		}

		private void DisplayScore()
		{
			if (m_UseCurrentSource) {
				m_DisplayText.text = DisplayFormat.Replace("{VALUE}", m_LevelData.Score.Score.ToString()).Replace("{BONUS_RATIO}", m_LevelData.Score.BonusRatio.ToString("0.000"));
			} else {
				m_DisplayText.text = DisplayFormat.Replace("{VALUE}", m_PlaythroughData.TotalScore.ToString()).Replace("{BONUS_RATIO}", "");
			}
		}

		private void DisplayBonusRatio()
		{
			m_DisplayText.text = m_UseCurrentSource ? DisplayFormat.Replace("{VALUE}", m_LevelData.Score.BonusRatio.ToString("0.000")) : "";
		}

		private void DisplayBlocksCleared()
		{
			m_DisplayText.text = DisplayFormat.Replace("{VALUE}", m_LevelData.Score.TotalClearedBlocksCount.ToString());
		}

		private void DisplayPlayTime()
		{
			float seconds = m_UseCurrentSource ? m_LevelData.PlayTime : m_PlaythroughData.TotalPlayTime;
			TimeSpan ts = new TimeSpan(0, 0, 0, (int) seconds, (int) Mathf.Round((seconds % 1) * 1000));

			string format = DisplayFormat.Replace("{totalSeconds}", $"'{((int)seconds)}'");
			m_DisplayText.text = ts.ToString(format);
		}
	}

}