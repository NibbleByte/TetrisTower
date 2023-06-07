#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Put next to or under a <see cref="Button"/> or <see cref="Toggle"/> or any <see cref="Selectable"/> component to get invoked on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeySelectableScopeElement : HotkeyBaseScopeElement
	{
		private Selectable m_Selectable;

		protected override void OnInvoke()
		{
			if (m_Selectable == null) {
				m_Selectable = GetComponentInParent<Selectable>();
			}

			ExecuteEvents.Execute(m_Selectable.gameObject, new PointerEventData(m_PlayerContext.EventSystem), ExecuteEvents.pointerClickHandler);
			// Button.onClick.Invoke(); // This will ignore disabled state.
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			// OnValidate() gets called even if object is not active.
			var selectable = GetComponentInParent<Selectable>(true);
			if (selectable == null) {
				Debug.LogError($"[Input] No valid button was found for HotkeyButton {name}", this);
				return;
			}

			var button = selectable as Button;
			if (button) {
				int eventCount = button.onClick.GetPersistentEventCount();
				if (eventCount == 0) {
					// User may subscribe dynamically runtime.
					//Debug.LogError($"[Input] Button {button.name} doesn't do anything on click, so it's hotkey will do nothing.", this);
					return;
				}

				for (int i = 0; i < eventCount; ++i) {
					if (button.onClick.GetPersistentTarget(i) == null) {
						Debug.LogError($"[Input] Button {button.name} has invalid target for on click event.", this);
						return;
					}

					if (string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(i))) {
						Debug.LogError($"[Input] Button {button.name} has invalid target method for on click event.", this);
						return;
					}
				}
			}

		}
	}
}

#endif