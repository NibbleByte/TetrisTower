using DevLocker.GFrame;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : ILevelState, PlayerControls.ITowerLevelPausedActions
	{
		private GridLevelController m_LevelController;
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_UIController);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);
			m_PlayerControls.TowerLevelPaused.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.Paused);

			m_LevelController.PauseLevel();

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_LevelController.ResumeLevel();

			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}
	}
}