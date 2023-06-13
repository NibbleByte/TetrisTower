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
		public UnityEvent OnAction;

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			OnAction.Invoke();
		}
	}
}

#endif