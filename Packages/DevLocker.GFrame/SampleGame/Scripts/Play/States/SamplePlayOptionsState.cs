using DevLocker.GFrame.Input;
using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Options is displayed.
	/// </summary>
	public class SamplePlayOptionsState : IPlayerState
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayUIController m_UIController;

		private InputEnabler m_InputEnabler;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.Sample_UI);

			m_UIController.SwitchState(PlayUIState.Options);
		}

		public void ExitState()
		{
			m_InputEnabler.Dispose();
		}
	}
}