using DevLocker.GFrame.MessageBox;
using TetrisTower.Game;
using TetrisTower.Logic;
using DevLocker.GFrame.Input;
using TetrisTower.TowerUI;
using System.Linq;
using DevLocker.GFrame;
using TetrisTower.TowerLevels.Replays;

namespace TetrisTower.TowerLevels
{
	public class TowerReplayPlaybackState : IPlayerState, IUpdateListener
	{
		private IPlaythroughData m_PlaythroughData;
		private IPlayerContext m_PlayerContext;
		private PlayerControls m_PlayerControls;
		private IInputContext m_InputContext;
		private GridLevelController m_LevelController;
		private TowerLevelUIController m_UIController;
		private LevelReplayPlayback m_PlaybackComponent;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_PlayerContext);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_UIController);
			context.SetByType(out m_InputContext);
			context.SetByType(out m_PlaybackComponent);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Submit);
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(TowerLevelUIState.Play);
			m_UIController.SetIsLevelPlaying(m_LevelController.LevelData.IsPlaying);

			MessageBox.Instance.MessageShown += PauseLevel;
			MessageBox.Instance.MessageClosed += ResumeLevel;

			// Disable player input during playback.
			var inputActions = m_PlayerControls.Where(a => a.actionMap != m_PlayerControls.TowerLevelPlay.Get());
			m_InputContext.PushOrSetActionsMask(this, inputActions);
		}

		public void ExitState()
		{
			m_PlayerControls.DisableAll(this);

			m_InputContext.PopActionsMask(this);

			MessageBox.Instance.MessageShown -= PauseLevel;
			MessageBox.Instance.MessageClosed -= ResumeLevel;
		}

		private void PauseLevel()
		{
			m_PlaythroughData.PausePlayers(playerWithInputPreserved: m_PlayerContext);
		}

		private void ResumeLevel()
		{
			m_PlaythroughData.ResumePlayers();
		}

		public void Update()
		{
			if (m_LevelController.LevelData != null && !m_LevelController.LevelData.IsPlaying) {
				// Check for this in Update, rather than the event, in case the level finished while in another state.
				m_PlayerContext.StatesStack.SetState(m_LevelController.LevelData.HasWon ? new TowerWonState() : new TowerLostState());
				return;
			}

			if (m_PlaybackComponent.PlaybackFinished && !MessageBox.Instance.IsShowingMessage) {

				if (m_PlaybackComponent.PlaybackInterruptionReason != LevelReplayPlayback.InterruptReason.None) {
					MessageBox.Instance.ShowSimple(
						"Replay Desynced",
						$"Replay playback was stopped because a desynchronization was detected.\nReason: {m_PlaybackComponent.PlaybackInterruptionReason}\n\nCheck the logs for more info.",
						MessageBoxIcon.Error,
						MessageBoxButtons.OK,
						() => m_PlayerContext.StatesStack.SetState(new TowerFinishedLevelState()),
						this
						);

				} else {
					m_PlayerContext.StatesStack.SetState(new TowerFinishedLevelState());
				}

				return;
			}
		}
	}
}