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

#if GFRAME_ASYNC
		public Task EnterStateAsync(PlayerStatesContext context)
#else
		public IEnumerator EnterState(PlayerStatesContext context)
#endif
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);

			m_UIController.SwitchState(PlayUIState.Options);

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
		}

#if GFRAME_ASYNC
		public Task ExitStateAsync()
#else
		public IEnumerator ExitState()
#endif
		{
			m_InputEnabler.Dispose();

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
		}
	}
}