using DevLocker.GFrame.Input;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerUI;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerFinishedLevelState : IPlayerState
	{
		private IPlaythroughData m_PlaythroughData;
		private GridLevelController m_LevelController;
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;

		private bool m_MouseAdded = false;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);

			// Multiplayer losers don't get UI.
			// If replay is not fully finished, IsPlaying should still be true. Each player open their UIs in that case.
			if (m_PlaythroughData.IsSinglePlayer || m_LevelController.LevelData.HasWon || m_LevelController.LevelData.IsPlaying) {
				m_PlayerControls.Enable(this, m_PlayerControls.UI);

				m_UIController.SwitchState(TowerLevelUIState.LevelFinished);
				// Don't pause the level as it will interrupt the won bonus animation.
			}

			// In split screen, each player gets their own device. But when using menus, it's best to provide the mouse.
			if (Mouse.current != null && m_PlayerControls.PairedDevices.Count > 0 && !m_PlayerControls.PairedDevices.OfType<Mouse>().Any()) {
				m_PlayerControls.PerformPairingWithDevice(Mouse.current);
				m_MouseAdded = true;
			}
		}

		public void ExitState()
		{
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