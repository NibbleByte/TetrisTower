using DevLocker.GFrame;
using System.Collections;
using TetrisTower.Game;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerFinishedLevelState : ILevelState
	{
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.LevelFinished);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}