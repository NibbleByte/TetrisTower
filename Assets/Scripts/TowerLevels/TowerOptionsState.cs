using DevLocker.GFrame;
using System.Collections;
using TetrisTower.Game;

namespace TetrisTower.TowerLevels
{
	public class TowerOptionsState : ILevelState
	{
		private TowerLevelUIController m_UIController;
		private PlayerControls m_PlayerControls;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.CommonHotkeys.Enable();

			m_UIController.SwitchState(TowerLevelUIState.Options);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}