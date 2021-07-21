using DevLocker.GFrame.SampleGame.Game;
using System.Collections;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Options is displayed.
	/// </summary>
	public class SamplePlayOptionsState : ILevelState
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_UIController);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();

			m_UIController.SwitchState(PlayUIState.Options);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}