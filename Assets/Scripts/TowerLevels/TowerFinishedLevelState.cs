using DevLocker.GFrame.Input;
using TetrisTower.Game;
using TetrisTower.TowerUI;

namespace TetrisTower.TowerLevels
{
	public class TowerFinishedLevelState : IPlayerState
	{
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);

			m_UIController.SwitchState(TowerLevelUIState.LevelFinished);
		}

		public void ExitState()
		{
			m_PlayerControls.DisableAll(this);
		}
	}
}