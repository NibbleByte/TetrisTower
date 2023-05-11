using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;

namespace TetrisTower.TowerLevels
{
	public class TowerOptionsState : IPlayerState
	{
		private UI.TowerLevelUIController m_UIController;
		private PlayerControls m_PlayerControls;

		private InputEnabler m_InputEnabler;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);

			m_UIController.SwitchState(UI.TowerLevelUIState.Options);
		}

		public void ExitState()
		{
			m_InputEnabler.Dispose();
		}
	}
}