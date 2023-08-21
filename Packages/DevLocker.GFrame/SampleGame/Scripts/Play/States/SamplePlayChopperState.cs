using DevLocker.GFrame.Input;
using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Player is in chopper state - can move freely in all directions with gravity turned off.
	/// This state also controls what is displayed on the UI via the UIController.
	/// </summary>
	public class SamplePlayChopperState : IPlayerState, SamplePlayerControls.ISample_PlayChopperActions
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
			m_PlayerControls.Enable(this, m_PlayerControls.Sample_PlayChopper);
			m_PlayerControls.Sample_PlayChopper.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.Disable(this, m_PlayerControls.Sample_UI.Submit);
			m_PlayerControls.Disable(this, m_PlayerControls.Sample_UI.Navigate);

			m_UIController.SwitchState(PlayUIState.Play, false);
		}

		public void ExitState()
		{
			m_PlayerControls.Sample_PlayChopper.SetCallbacks(null);
			m_PlayerControls.DisableAll(this);
		}

		public void OnChopperMovement(InputAction.CallbackContext context)
		{
			m_PlayerController.ChopperMovement(context.ReadValue<Vector2>());
		}

		public void OnSwitchToJumper(InputAction.CallbackContext context)
		{
			m_PlayerController.SwitchToJumper();
		}
	}
}