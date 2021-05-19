#if USE_INPUT_SYSTEM

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.UIHotkeys
{
	/// <summary>
	/// Put next to or under a UI.Button component to get invoked on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyButton : MonoBehaviour, IHotkeyElement
	{
		[SerializeField]
		private InputActionReference m_InputAction;

		private Button m_Button;

		void OnEnable()
		{
			var controls = (LevelsManager.Instance.GameContext as IInputActionsProvider)?.Controls;

			if (controls == null) {
				Debug.LogWarning($"{nameof(HotkeyButton)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			controls.FindAction(m_InputAction.name).performed += OnInputAction;
		}

		void OnDisable()
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var controls = (LevelsManager.Instance.GameContext as IInputActionsProvider)?.Controls;

			if (controls == null) {
				return;
			}

			controls.FindAction(m_InputAction.name).performed -= OnInputAction;
		}

		private void OnInputAction(InputAction.CallbackContext obj)
		{
			if (m_Button == null) {
				m_Button = GetComponentInParent<Button>();
			}

			m_Button.onClick.Invoke();
		}

		void OnValidate()
		{
			// OnValidate() gets called even if object is not active.
			// HACK: Because Unity are idiots and missed this overload.
			//var button = GetComponentInParent<Button>(true);

			Button button = null;
			var tr = transform;
			while (tr) {
				button = tr.GetComponent<Button>();
				if (button)
					break;

				tr = tr.parent;
			}


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