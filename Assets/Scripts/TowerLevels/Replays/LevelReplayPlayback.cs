using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Timing;
using TetrisTower.Game;
using TetrisTower.TowerUI;
using UnityEngine;

namespace TetrisTower.TowerLevels.Replays
{
	public class LevelReplayPlayback : MonoBehaviour, ILevelLoadedListener
	{
		public readonly WiseTiming Timing = new WiseTiming();

		private FlashMessageUIController m_FlashMessage;

		public ReplayRecording PlaybackRecording;
		private int m_PlaybackIndex = 0;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_FlashMessage);
		}

		public void OnLevelUnloading()
		{
		}

		void Update()
		{
			if (PlaybackRecording.GridLevelController.IsPaused)
				return;

			if (m_PlaybackIndex == PlaybackRecording.Actions.Count) {
				string currentState = PlaybackRecording.GetSavedState(PlaybackRecording.GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);
				if (currentState != PlaybackRecording.FinalState) {
					m_FlashMessage.ShowMessage("Replay Desynced", false);

					Debug.LogError($"Replay current state doesn't match the final playback state. Compared states:", this);
					Debug.LogError(currentState, this);
					Debug.LogError(PlaybackRecording.FinalState, this);
				} else {
					Debug.Log($"Replay playback finished succesfully!", this);
				}

				enabled = false;
				return;
			}

			while (m_PlaybackIndex < PlaybackRecording.Actions.Count) {
				ReplayAction action = PlaybackRecording.Actions[m_PlaybackIndex];
				m_PlaybackIndex++;

				action.Replay(PlaybackRecording.GridLevelController);

				// Continue next frame.
				if (action.ActionType == ReplayActionType.Update)
					return;

				// Don't show if last action - it's probably when the user paused the game to end the replay record. Equals count as ++ above.
				if (action.ActionType == ReplayActionType.Pause && m_PlaybackIndex != PlaybackRecording.Actions.Count) {
					m_FlashMessage.ShowMessage("Pause Skipped");
				}
			}
		}
	}
}