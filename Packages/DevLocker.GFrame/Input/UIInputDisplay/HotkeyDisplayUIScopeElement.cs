#if USE_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Displays hotkey icon / text.
	/// Refreshes if devices change.
	/// Will be controlled by UIScope.
	/// </summary>
	public class HotkeyDisplayUIScopeElement : HotkeyDisplayUI, UIScope.IScopeElement, UIScope.IHotkeysWithInputActions
	{
		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (m_InputAction == null)
				yield break;

			InputAction action = inputContext.FindActionFor(m_InputAction.name);
			if (action != null) {
				yield return action;
			}
		}
	}

}
#endif