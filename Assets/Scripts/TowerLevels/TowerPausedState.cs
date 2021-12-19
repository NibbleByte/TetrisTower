using DevLocker.GFrame;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : ILevelState, PlayerControls.ITowerLevelPausedActions
	{
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);
			m_PlayerControls.TowerLevelPaused.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.Paused);

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}
	}
}