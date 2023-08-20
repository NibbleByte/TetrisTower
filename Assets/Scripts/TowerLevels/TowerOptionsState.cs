using DevLocker.GFrame.Input;
using TetrisTower.Game;
using TetrisTower.TowerUI;

namespace TetrisTower.TowerLevels
{
	public class TowerOptionsState : IPlayerState
	{
		private TowerLevelUIController m_UIController;
		private PlayerControls m_PlayerControls;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);

			m_UIController.SwitchState(TowerLevelUIState.Options);
		}

		public void ExitState()
		{
			m_PlayerControls.Disable(this);
		}
	}
}