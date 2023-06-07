#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.Events;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyFocusScopeElement : HotkeyBaseScopeElement
	{
		public UIScope FocusScope;

		protected override void OnInvoke()
		{
			if (FocusScope == null) {
				FocusScope = GetComponentInParent<UIScope>();
			}

			if (FocusScope == null) {
				Debug.LogWarning($"[Input] No scope to focus for {name}", this);
				return;
			}

			FocusScope.Focus();
		}
	}
}

#endif