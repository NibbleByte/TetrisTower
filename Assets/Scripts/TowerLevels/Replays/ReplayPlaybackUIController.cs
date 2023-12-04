using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.TowerLevels.Replays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerUI
{
	// HACK: This should be in the TowerUI folder, but because of dependency hell, it's not. :D
	public class ReplayPlaybackUIController : MonoBehaviour, ILevelLoadedListener
	{
		private LevelReplayPlayback m_PlaybackComponent;

		public Button PlaybackSpeedButton;


		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.TrySetByType(out m_PlaybackComponent);

			PlaybackSpeedButton.onClick.AddListener(OnPlaybackSpeedClicked);

			if (m_PlaybackComponent == null)
				return;

			PlaybackSpeedButton.GetComponentInChildren<TMP_Text>().text = $"x{m_PlaybackComponent.PlaybackSpeed}";
		}

		public void OnLevelUnloading()
		{
			PlaybackSpeedButton.onClick.RemoveListener(OnPlaybackSpeedClicked);
		}

		private void OnPlaybackSpeedClicked()
		{
			m_PlaybackComponent.PlaybackSpeed = (m_PlaybackComponent.PlaybackSpeed * 2) % 7; // 1, 2, 4
			PlaybackSpeedButton.GetComponentInChildren<TMP_Text>().text = $"x{m_PlaybackComponent.PlaybackSpeed}";
		}
	}

}