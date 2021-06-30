#if USE_INPUT_SYSTEM
using UnityEngine;

namespace DevLocker.GFrame.UIInputDisplay
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