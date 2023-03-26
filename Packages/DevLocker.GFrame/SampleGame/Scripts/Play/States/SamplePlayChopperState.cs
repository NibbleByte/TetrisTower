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
	public class SamplePlayChopperState : IPlayerState, SamplePlayerControls.IPlayChopperActions
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayerController m_PlayerController;
		private SamplePlayUIController m_UIController;

		private InputEnabler m_InputEnabler;

#if GFRAME_ASYNC
		public Task EnterStateAsync(PlayerStatesContext context)
#else
		public IEnumerator EnterState(PlayerStateContext context)
#endif
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_PlayerController);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.PlayChopper);
			m_PlayerControls.PlayChopper.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_InputEnabler.Disable(m_PlayerControls.UI.Submit);
			m_InputEnabler.Disable(m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(PlayUIState.Play, false);

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
			m_PlayerControls.PlayChopper.SetCallbacks(null);
			m_InputEnabler.Dispose();

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
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