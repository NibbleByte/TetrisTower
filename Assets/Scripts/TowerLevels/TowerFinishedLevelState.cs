using DevLocker.GFrame;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerFinishedLevelState : ILevelState
	{
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.LevelFinished);

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}
	}
}