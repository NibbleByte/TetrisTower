#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyEventScopeElement : HotkeyBaseScopeElement
	{
		[UnityEngine.Serialization.FormerlySerializedAs("OnAction")]
		public UnityEvent OnPerformed;

		public UnityEvent OnStarted;

		public UnityEvent OnCancelled;

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			OnPerformed.Invoke();
		}

		protected override void OnStart(InputAction.CallbackContext context)
		{
			OnStarted.Invoke();
		}

		protected override void OnCancel(InputAction.CallbackContext context)
		{
			OnCancelled.Invoke();
		}
	}
}

#endif