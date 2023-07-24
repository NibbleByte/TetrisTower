using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using TetrisTower.Game;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class PlayTimeDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		private GridLevelController m_TowerLevel;

		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private IPlaythroughData m_PlaythroughData;

		[Tooltip("Use the C# TimeStamp formatting. Use {totalSeconds} to display time in seconds (no minutes).\nNOTE: use '' for arbitrary text.")]
		public string DisplayFormat = @"'Play time: 'mm\:ss\.fff";

		[Tooltip("If true will display total play through instead of just current level play time.")]
		public bool PlaythroughTime = false;

		public TextMeshProUGUI DisplayText;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_TowerLevel);
			context.SetByType(out m_PlaythroughData);
		}

		public void OnLevelUnloading()
		{
		}

		void Update()
		{
			float seconds = PlaythroughTime ? m_PlaythroughData.TotalPlayTime : m_LevelData.PlayTime;
			TimeSpan ts = new TimeSpan(0, 0, 0, (int) seconds, (int) Mathf.Round((seconds % 1) * 1000));

			string format = DisplayFormat.Replace("{totalSeconds}", $"'{((int)seconds)}'");
			DisplayText.text = ts.ToString(format);
		}
	}

}