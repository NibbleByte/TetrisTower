using DevLocker.GFrame;
using System.Collections;
using TetrisTower.Input;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : ILevelState, PlayerControls.ITowerLevelPausedActions
	{
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);

			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);
			m_PlayerControls.TowerLevelPaused.Enable();

			m_UIController.SwitchState(TowerLevelUIState.Paused);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_PlayerControls.TowerLevelPaused.Disable();

			yield break;
		}
	}
}