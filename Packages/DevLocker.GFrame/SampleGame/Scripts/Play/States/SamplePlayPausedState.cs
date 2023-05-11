using DevLocker.GFrame.Input;
using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Game is paused - menu is shown.
	/// </summary>
	public class SamplePlayPausedState : IPlayerState
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayUIController m_UIController;

		private InputEnabler m_InputEnabler;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);

			m_UIController.SwitchState(PlayUIState.Paused);
		}

		public void ExitState()
		{
			m_InputEnabler.Dispose();
		}
	}
}