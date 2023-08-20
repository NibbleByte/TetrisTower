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

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_PlayerControls.Enable(this, m_PlayerControls.Sample_UI);

			m_UIController.SwitchState(PlayUIState.Paused);
		}

		public void ExitState()
		{
			m_PlayerControls.Disable(this);
		}
	}
}