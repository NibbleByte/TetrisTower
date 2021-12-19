using DevLocker.GFrame;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;

namespace TetrisTower.TowerLevels
{
	public class TowerOptionsState : ILevelState
	{
		private UI.TowerLevelUIController m_UIController;
		private PlayerControls m_PlayerControls;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.Options);

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}
	}
}