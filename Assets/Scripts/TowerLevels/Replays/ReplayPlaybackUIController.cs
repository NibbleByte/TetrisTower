using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.TowerLevels.Replays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerUI
{
	// HACK: This should be in the TowerUI folder, but because of dependency hell, it's not. :D
	public class ReplayPlaybackUIController : MonoBehaviour
	{
		public Button PlaybackSpeedButton;

		private int m_PlaybackSpeed = 1;
		private LevelReplayPlayback[] m_PlaybackComponents;

		public void Setup(IEnumerable<LevelReplayPlayback> playbackComponents)
		{
			m_PlaybackComponents = playbackComponents.ToArray();

			PlaybackSpeedButton.onClick.AddListener(OnPlaybackSpeedClicked);

			PlaybackSpeedButton.GetComponentInChildren<TMP_Text>().text = $"x{m_PlaybackSpeed}";
		}

		private void OnPlaybackSpeedClicked()
		{
			m_PlaybackSpeed = (m_PlaybackSpeed * 2) % 7; // 1, 2, 4
			PlaybackSpeedButton.GetComponentInChildren<TMP_Text>().text = $"x{m_PlaybackSpeed}";

			foreach(LevelReplayPlayback playback in m_PlaybackComponents) {
				playback.PlaybackSpeed = m_PlaybackSpeed;
			}
		}
	}

}