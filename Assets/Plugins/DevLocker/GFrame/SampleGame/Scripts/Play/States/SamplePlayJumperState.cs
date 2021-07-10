using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.Play
{
	public class SamplePlayJumperState : ILevelState, SamplePlayerControls.IPlayJumperActions
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayerController m_PlayerController;
		private SamplePlayUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_PlayerController);
			contextReferences.SetByType(out m_UIController);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.PlayJumper.SetCallbacks(this);
			m_PlayerControls.PlayJumper.Enable();

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.UI.Submit.Disable();
			m_PlayerControls.UI.Navigate.Disable();

			m_UIController.SwitchState(PlayUIState.Play);

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.PlayJumper.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}

		public void OnJumperMovement(InputAction.CallbackContext context)
		{
			m_PlayerController.JumperMovement(context.ReadValue<float>());
		}

		public void OnJumperJump(InputAction.CallbackContext context)
		{
			m_PlayerController.JumperJump();
		}

		public void OnSwitchToChopper(InputAction.CallbackContext context)
		{
			m_PlayerController.SwitchToChopper();
		}
	}
}