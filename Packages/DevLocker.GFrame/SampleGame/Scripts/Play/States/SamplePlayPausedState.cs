using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Game is paused - menu is shown.
	/// </summary>
	public class SamplePlayPausedState : ILevelState
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayUIController m_UIController;

#if GFRAME_ASYNC
		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
#else
		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
#endif
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_UIController);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(PlayUIState.Paused);

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
			m_PlayerControls.InputStack.PopActionsState(this);

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
		}
	}
}