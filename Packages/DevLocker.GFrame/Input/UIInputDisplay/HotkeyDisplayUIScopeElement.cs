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
	public class HotkeyDisplayUIScopeElement : HotkeyDisplayUI, UIScope.IScopeElement
	{

	}

}
#endif