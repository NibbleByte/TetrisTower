#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Put next to or under a UI.Button component to get invoked on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyButtonScopeElement : HotkeyBaseScopeElement
	{
		private Button m_Button;

		protected override void OnInvoke()
		{
			if (m_Button == null) {
				m_Button = GetComponentInParent<Button>();
			}

			ExecuteEvents.Execute(m_Button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
			// Button.onClick.Invoke(); // This will ignore disabled state.
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			// OnValidate() gets called even if object is not active.
			var button = GetComponentInParent<Button>(true);
			if (button == null) {
				Debug.LogError($"No valid button was found for HotkeyButton {name}", this);
				return;
			}

			int eventCount = button.onClick.GetPersistentEventCount();
			if (eventCount == 0) {
				// User may subscribe dynamically runtime.
				//Debug.LogError($"Button {button.name} doesn't do anything on click, so it's hotkey will do nothing.", this);
				return;
			}

			for(int i = 0; i < eventCount; ++i) {
				if (button.onClick.GetPersistentTarget(i) == null) {
					Debug.LogError($"Button {button.name} has invalid target for on click event.", this);
					return;
				}

				if (string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(i))) {
					Debug.LogError($"Button {button.name} has invalid target method for on click event.", this);
					return;
				}
			}

		}
	}
}

#endif