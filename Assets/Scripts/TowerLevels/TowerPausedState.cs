using TetrisTower.Core;
using TetrisTower.Input;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : ILevelState, PlayerControls.ITowerLevelUIActions
	{
		private PlayerControls m_PlayerControls;
		private TowerLevelController m_LevelController;
		private TowerLevelUIController m_UIController;

		public void EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_LevelController);

			m_PlayerControls.TowerLevelUI.SetCallbacks(this);
			m_PlayerControls.TowerLevelUI.Enable();
			m_PlayerControls.UI.Enable();
			m_UIController.ShowPausedPanel(true);

			m_LevelController.PauseLevel();
		}

		public void ExitState()
		{
			m_UIController.ShowPausedPanel(false);
			m_PlayerControls.TowerLevelUI.SetCallbacks(null);
			m_PlayerControls.TowerLevelUI.Disable();
			m_PlayerControls.UI.Disable();

			m_LevelController.ResumeLevel();
		}

		public void OnResumeLevel(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelSupervisorsManager.Instance.LevelStatesStack.SetState(new TowerPlayState());
			}
		}
	}
}