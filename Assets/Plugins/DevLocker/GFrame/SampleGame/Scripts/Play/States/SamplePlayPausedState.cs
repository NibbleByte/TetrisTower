using DevLocker.GFrame.SampleGame.Game;
using System.Collections;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Game is paused - menu is shown.
	/// </summary>
	public class SamplePlayPausedState : ILevelState
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_UIController);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(PlayUIState.Paused);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}