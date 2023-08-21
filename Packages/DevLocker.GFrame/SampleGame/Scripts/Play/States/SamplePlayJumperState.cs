using DevLocker.GFrame.Input;
using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Player is in jumper state - can move left and right + jump and is affected by gravity.
	/// This state also controls what is displayed on the UI via the UIController.
	/// </summary>
	public class SamplePlayJumperState : IPlayerState, SamplePlayerControls.ISample_PlayJumperActions
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayerController m_PlayerController;
		private SamplePlayUIController m_UIController;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_PlayerController);
			context.SetByType(out m_UIController);

			m_PlayerControls.Enable(this, m_PlayerControls.Sample_UI);
			m_PlayerControls.Enable(this, m_PlayerControls.Sample_PlayJumper);
			m_PlayerControls.Sample_PlayJumper.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.Disable(this, m_PlayerControls.Sample_UI.Submit);
			m_PlayerControls.Disable(this, m_PlayerControls.Sample_UI.Navigate);

			m_UIController.SwitchState(PlayUIState.Play, true);
		}

		public void ExitState()
		{
			m_PlayerControls.Sample_PlayJumper.SetCallbacks(null);
			m_PlayerControls.DisableAll(this);
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