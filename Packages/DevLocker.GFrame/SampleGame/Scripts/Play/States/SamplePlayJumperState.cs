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
	public class SamplePlayJumperState : ILevelState, SamplePlayerControls.IPlayJumperActions
	{
		private SamplePlayerControls m_PlayerControls;
		private SamplePlayerController m_PlayerController;
		private SamplePlayUIController m_UIController;

		private InputEnabler m_InputEnabler;

#if GFRAME_ASYNC
		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
#else
		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
#endif
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_PlayerController);
			contextReferences.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.PlayJumper);
			m_PlayerControls.PlayJumper.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_InputEnabler.Disable(m_PlayerControls.UI.Submit);
			m_InputEnabler.Disable(m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(PlayUIState.Play, true);

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
			m_PlayerControls.PlayJumper.SetCallbacks(null);
			m_InputEnabler.Dispose();

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
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