using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Replays;
using TetrisTower.TowerUI;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : IPlayerState, PlayerControls.ITowerLevelPausedActions
	{
		private IPlaythroughData m_PlaythroughData;
		private IPlayerContext m_PlayerContext;
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;
		private ReplayActionsRecording m_ReplayRecording;

		private bool m_MouseAdded = false;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_PlayerContext);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);
			context.TrySetByType(out m_ReplayRecording);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);
			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);

			m_UIController.SwitchState(TowerLevelUIState.Paused);

			m_PlaythroughData.PausePlayers(playerWithInputPreserved: m_PlayerContext, this);

			// On replay playback, this will be null.
			if (m_ReplayRecording != null) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Pause);
			}

			// In split screen, each player gets their own device. But when using menus, it's best to provide the mouse.
			if (Mouse.current != null && m_PlayerControls.PairedDevices.Count > 0 && !m_PlayerControls.PairedDevices.OfType<Mouse>().Any()) {
				m_PlayerControls.PerformPairingWithDevice(Mouse.current);
				m_MouseAdded = true;
			}
		}

		public void ExitState()
		{
			m_PlaythroughData.ResumePlayers(this);

			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_PlayerControls.DisableAll(this);

			if (m_MouseAdded) {
				// Mouse should not dissapear, but just in case?
				if (Mouse.current != null) {
					m_PlayerControls.UnpairDevice(Mouse.current);
				}
				m_MouseAdded = false;
			}
		}
	}
}