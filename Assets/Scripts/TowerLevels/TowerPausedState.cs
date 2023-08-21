using DevLocker.GFrame.Input;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerUI;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : IPlayerState, PlayerControls.ITowerLevelPausedActions
	{
		private GridLevelController m_LevelController;
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelController);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);
			m_PlayerControls.Enable(this, m_PlayerControls.TowerLevelPaused);
			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);

			m_UIController.SwitchState(TowerLevelUIState.Paused);

			m_LevelController.PauseLevel();
		}

		public void ExitState()
		{
			m_LevelController.ResumeLevel();

			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_PlayerControls.DisableAll(this);
		}
	}
}