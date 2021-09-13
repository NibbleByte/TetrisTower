#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.Events;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyEventScopeElement : HotkeyBaseScopeElement
	{
		[SerializeField]
		private UnityEvent m_OnAction;

		protected override void OnInvoke()
		{
			m_OnAction.Invoke();
		}
	}
}

#endif