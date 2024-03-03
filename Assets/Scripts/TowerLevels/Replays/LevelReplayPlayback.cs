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
		public enum InterruptReason
		{
			None,
			DesyncDetected,
			LevelEndedBeforeReplay,
			FinalStateMismatch,
		}

		public readonly WiseTiming Timing = new WiseTiming();

		private FlashMessageUIController m_FlashMessage;

		public int PlaybackSpeed = 1;
		public int PlayerIndex = -1;

		public ReplayActionsRecording PlayerPlaybackRecording;
		private int m_PlaybackIndex = 0;

		public bool PlaybackFinished { get; private set; } = false;

		public InterruptReason PlaybackInterruptionReason { get; private set; }

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_FlashMessage);
		}

		public void OnLevelUnloading()
		{
		}

		private void EndPlayback()
		{
			string currentState = PlayerPlaybackRecording.GetSavedState(PlayerPlaybackRecording.GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);
			if (currentState != PlayerPlaybackRecording.FinalState) {
				//m_FlashMessage.ShowMessage("Replay Desynced", false);	// MessageBox is displayed by the state.

				Debug.LogError($"Replay current state for player {PlayerIndex} doesn't match the final recorded state. Compared states: (actual, recorded)", this);
				Debug.LogError(currentState, this);
				Debug.LogError(PlayerPlaybackRecording.FinalState, this);

				PlaybackInterruptionReason = InterruptReason.FinalStateMismatch;

				enabled = false;

			} else {

				if (PlayerPlaybackRecording.GridLevelController.LevelData.IsPlaying) {
					// Ended replay from paused menu while playing.
					Debug.Log($"Replay playback finished succesfully for player {PlayerIndex}, but level did not! Stop further playback.", this);
					enabled = false;
				} else {
					Debug.Log($"Replay playback finished succesfully for player {PlayerIndex}!", this);
				}
			}

			PlaybackFinished = true;
		}

		private void InterruptPlayback(InterruptReason reason, ReplayAction interruptAction, int actionIndex, float expectedValue, float resultValue)
		{
			string currentState = PlayerPlaybackRecording.GetSavedState(PlayerPlaybackRecording.GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);

			//m_FlashMessage.ShowMessage("Replay Desynced", false); // MessageBox is displayed by the state.

			Debug.LogError($"Replay interrupted - {reason} for player {PlayerIndex}. Action: {interruptAction.ActionType} at {actionIndex}; Value: {interruptAction.Value}; Expected Value: {expectedValue}; Found Value: {resultValue}. Current state:", this);
			Debug.LogError(currentState, this);

			PlaybackInterruptionReason = reason;

			enabled = false;

			PlaybackFinished = true;
		}

		void Update()
		{
			if (PlayerPlaybackRecording.GridLevelController.IsPaused)
				return;

			if (PlaybackFinished) {
				Timing.UpdateCoroutines(Time.deltaTime);
				return;
			}

			int startPlaybackIndex = m_PlaybackIndex;

			while (m_PlaybackIndex < PlayerPlaybackRecording.Actions.Count) {
				ReplayAction action = PlayerPlaybackRecording.Actions[m_PlaybackIndex];

				if (action.ActionType == ReplayActionType.RecordingEnd) {
					EndPlayback();
					return;
				}

				m_PlaybackIndex++;

				// Because we ++ above, index is now pointing to the next one.
				ReplayActionType nextType = m_PlaybackIndex < PlayerPlaybackRecording.Actions.Count
					? PlayerPlaybackRecording.Actions[m_PlaybackIndex].ActionType
					: default;

				// Expected value will be overriden during replay action.
				float expectedValue = action.ExpectedResultValue;

				action.Replay(PlayerPlaybackRecording.GridLevelController, PlayerPlaybackRecording.Fairy);

				if (action.ExpectedResultValue != expectedValue) {
					InterruptPlayback(InterruptReason.DesyncDetected, action, m_PlaybackIndex - 1, expectedValue, action.ExpectedResultValue);
					return;
				}

				// Yield next frame if it is a gameplay action. If ending, puase, etc. - run immediately or the Won animation will change the state.
				if (action.ActionType == ReplayActionType.Update && nextType < ReplayActionType.Pause) {
					if (m_PlaybackIndex - startPlaybackIndex < PlaybackSpeed) {
						continue;
					} else {
						return;
					}
				}

				// If playback finishes before replay end, don't continue, display desync right away.
				if (!PlayerPlaybackRecording.GridLevelController.LevelData.IsPlaying && nextType != ReplayActionType.RecordingEnd) {
					InterruptPlayback(InterruptReason.LevelEndedBeforeReplay, action, m_PlaybackIndex - 1, expectedValue, action.ExpectedResultValue);
					return;
				}

				switch (action.ActionType) {
					case ReplayActionType.Pause:
						// Don't show if last action (right before ending) - it's probably when the user paused the game to end the replay record.
						// NOTE: m_PlaybackIndex is the next action at the moment, because of the ++ above.
						if (m_PlaybackIndex >= PlayerPlaybackRecording.Actions.Count || nextType != ReplayActionType.RecordingEnd) {
							m_FlashMessage.ShowMessage("Pause Skipped");
						}
						break;

					case ReplayActionType.Cheat_Generic:
						m_FlashMessage.ShowMessage("Cheat Used!");
						break;

					case ReplayActionType.Cheat_EndLevel:
						m_FlashMessage.ShowMessage("Cheat End Level!");
						break;
				}
			}
		}
	}
}