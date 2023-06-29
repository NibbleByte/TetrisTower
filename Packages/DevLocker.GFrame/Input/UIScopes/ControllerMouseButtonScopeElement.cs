#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Put next to a <see cref="Button"/> component to get invoked on specified InputAction.
	/// With correct setup, when using controller, button will remain hidden while controller hotkey hint appears.
	/// Note that this action has to be enabled in order to be invoked.
	///
	/// The user code should work with the button, while this script takes care of the display and hotkeys.
	/// Under the hood it disables the button image component (but the button itself remains active, just invisible) when controller is used.
	/// If button is set as non-interactable or the owner scope is inactive, the controller variant remains hidden.
	/// If the button has <see cref="LayoutElement"/> component, it will set <see cref="LayoutElement.ignoreLayout"/> flag to true for controller,
	/// so layout groups ignore it, but will be reset on mouse & keyboard.
	/// </summary>
	public class ControllerMouseButtonScopeElement : HotkeyBaseScopeElement
	{
		private Button m_Button;

		[Tooltip("Optional layout element to have ignore flag set when not interactable for controller. Useful with layout groups.")]
		public LayoutElement LayoutElement;

		[Tooltip("Elements that are part of the mouse & keyboard button that should be hidden while using controller.")]
		public List<GameObject> MouseButtonParts;
		[Tooltip("Elements that are part of the controller hotkey that should be hidden while using mouse & keyboard.")]
		public List<GameObject> ControllerHotkeyParts;

		private bool m_LastInteractable = false;

		protected override void OnContextReady()
		{
			base.OnContextReady();

			m_PlayerContext.InputContext.LastUsedDeviceChanged += OnLastUsedDeviceChanged;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (!m_PlayerContext.IsActive || !m_HasInitialized)
				return;

			m_PlayerContext.InputContext.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (!m_HasInitialized)
				return;

			OnLastUsedDeviceChanged();
		}

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			if (m_Button == null) {
				m_Button = GetComponentInParent<Button>();
			}

			if (m_Button.IsInteractable() && m_Button.isActiveAndEnabled) {
				m_Button.onClick.Invoke();
			}
		}

		private void OnLastUsedDeviceChanged()
		{
			if (m_Button == null) {
				m_Button = GetComponentInParent<Button>(true);
			}

			if (m_Button == null)
				return;

			m_LastInteractable = m_Button.IsInteractable() && m_Button.enabled;

			bool isController = m_PlayerContext.InputContext.GetLastUsedInputDevice() is Gamepad;

			foreach(GameObject go in MouseButtonParts) {
				go.SetActive(!isController);
			}

			foreach(GameObject go in ControllerHotkeyParts) {
				// If owner scope is inactive, this component is disabled, therefore hide the hotkeys, as focus is somewhere else.
				go.SetActive(isController && m_LastInteractable && enabled);
			}

			m_Button.image.enabled = !isController;
			m_Button.navigation = new Navigation() { mode = Navigation.Mode.None };

			if (LayoutElement) {
				LayoutElement.ignoreLayout = isController && (!m_LastInteractable || !enabled);
			}
		}

		private void Update()
		{
			if (m_Button) {
				bool interactable = m_Button.IsInteractable() && m_Button.enabled;
				if (interactable != m_LastInteractable) {
					OnLastUsedDeviceChanged();
				}
			}
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			// OnValidate() gets called even if object is not active.
			var button = GetComponentInParent<Button>(true);
			if (button == null) {
				Debug.LogError($"[Input] No valid button was found for HotkeyButton {name}", this);
				return;
			}

#if UNITY_EDITOR
			if (MouseButtonParts.Contains(button.gameObject)) {
				MouseButtonParts.Remove(button.gameObject);
				UnityEditor.EditorUtility.SetDirty(this);
			}
			if (MouseButtonParts.Contains(gameObject)) {
				MouseButtonParts.Remove(gameObject);
				UnityEditor.EditorUtility.SetDirty(this);
			}

			if (ControllerHotkeyParts.Contains(button.gameObject)) {
				ControllerHotkeyParts.Remove(button.gameObject);
				UnityEditor.EditorUtility.SetDirty(this);
			}
			if (ControllerHotkeyParts.Contains(gameObject)) {
				ControllerHotkeyParts.Remove(gameObject);
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif

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

#endif