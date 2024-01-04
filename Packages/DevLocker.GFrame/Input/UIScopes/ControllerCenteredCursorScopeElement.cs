#if USE_INPUT_SYSTEM

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Displays controller cursor in the center of the view port, when controller device used.
	/// The cursor acts as a virtual mouse, triggering pointer hover effects & clicks.
	///
	/// Some copy-paste from <see cref="UnityEngine.InputSystem.UI.VirtualMouseInput"/>, which has issues with canvas scaler.
	/// </summary>
	public class ControllerCenteredCursorScopeElement : MonoBehaviour, IScopeElement
	{
		[Tooltip("Specify the controller scheme to show the cursor to.")]
		[InputControlSchemePicker]
		public string ControllerScheme;

		[Tooltip("Visuals to be displayed when the cursor is active.")]
		public GameObject CursorVisuals;

		[Tooltip("Should PC hardware cursor be hidden, when controller one is displayed?")]
		public bool HideHardwareCursor = true;

		// Copy-pasted from VirtualMouseInput
		[Tooltip("Button action that triggers a left-click on the mouse.")]
		[SerializeField] private InputActionProperty m_LeftButtonAction;
		[Tooltip("Button action that triggers a middle-click on the mouse.")]
		[SerializeField] private InputActionProperty m_MiddleButtonAction;
		[Tooltip("Button action that triggers a right-click on the mouse.")]
		[SerializeField] private InputActionProperty m_RightButtonAction;

		private Action<InputAction.CallbackContext> m_ButtonActionTriggeredDelegate;


		public Mouse VirtualMouse { get; private set; }
		private Canvas m_Canvas;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			m_PlayerContext.AddSetupCallback((delayedSetup) => {
				m_HasInitialized = true;

				if (VirtualMouse == null) {
					VirtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
					InputSystem.DisableDevice(VirtualMouse);
				}

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		void OnDestroy()
		{
			if (VirtualMouse != null) {
				InputSystem.RemoveDevice(VirtualMouse);
			}
		}

		void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			m_Canvas = GetComponentInParent<Canvas>();

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged += OnInputControlSchemeChanged;
			OnInputControlSchemeChanged();
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged -= OnInputControlSchemeChanged;
			SetControllerCursorState(false);
		}

		private void OnInputControlSchemeChanged()
		{
			bool isController = m_PlayerContext.InputContext.GetLastUsedInputControlScheme().name == ControllerScheme;
			SetControllerCursorState(isController);
		}

		private void SetControllerCursorState(bool active)
		{
			if (active && !VirtualMouse.enabled) {
				InputSystem.EnableDevice(VirtualMouse);
				InputSystem.onAfterUpdate += OnInputUpdated;

				// Invalidates cache, forcing position change.
				InputState.Change(VirtualMouse.position, new Vector2(-1f, -1f));

				// Copy-pasted from VirtualMouseInput
				// Hook into actions.
				if (m_ButtonActionTriggeredDelegate == null)
					m_ButtonActionTriggeredDelegate = OnButtonActionTriggered;
				SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, true);
				SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, true);
				SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, true);
				//SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, true);
				//SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, true);

				m_LeftButtonAction.action?.Enable();
				m_MiddleButtonAction.action?.Enable();
				m_RightButtonAction.action?.Enable();

			} else if (!active && VirtualMouse.enabled) {
				InputSystem.DisableDevice(VirtualMouse);
				InputSystem.onAfterUpdate -= OnInputUpdated;

				m_LeftButtonAction.action?.Disable();
				m_MiddleButtonAction.action?.Disable();
				m_RightButtonAction.action?.Disable();

				// Copy-pasted from VirtualMouseInput
				// Unhock from actions.
				if (m_ButtonActionTriggeredDelegate != null) {
					SetActionCallback(m_LeftButtonAction, m_ButtonActionTriggeredDelegate, false);
					SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, false);
					SetActionCallback(m_RightButtonAction, m_ButtonActionTriggeredDelegate, false);
					//SetActionCallback(m_MiddleButtonAction, m_ButtonActionTriggeredDelegate, false);
					//SetActionCallback(m_ForwardButtonAction, m_ButtonActionTriggeredDelegate, false);
					//SetActionCallback(m_BackButtonAction, m_ButtonActionTriggeredDelegate, false);
				}
			}

			if (CursorVisuals) {
				CursorVisuals.SetActive(active);
			}

			if (HideHardwareCursor) {
				Cursor.visible = !active;
			}
		}

		private void OnInputUpdated()
		{
			Rect viewPortRect = m_Canvas && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay && m_Canvas.worldCamera != null
					? m_Canvas.worldCamera.rect
					: new Rect(0, 0, 1f, 1f)
				;

			Vector2 viewportCenter;
			viewportCenter.x = viewPortRect.x * Screen.width + viewPortRect.width * Screen.width / 2f;
			viewportCenter.y = viewPortRect.y * Screen.height + viewPortRect.height * Screen.height / 2f;

			if (viewportCenter != VirtualMouse.position.ReadValue()) {
				InputState.Change(VirtualMouse.position, viewportCenter);
			}
		}

		// Copy-pasted from VirtualMouseInput
		private void OnButtonActionTriggered(InputAction.CallbackContext context)
		{
			if (VirtualMouse == null)
				return;

			// The button controls are bit controls. We can't (yet?) use InputState.Change to state
			// the change of those controls as the state update machinery of InputManager only supports
			// byte region updates. So we just grab the full state of our virtual mouse, then update
			// the button in there and then simply overwrite the entire state.

			var action = context.action;
			MouseButton? button = null;
			if (action == m_LeftButtonAction.action)
				button = MouseButton.Left;
			else if (action == m_RightButtonAction.action)
				button = MouseButton.Right;
			else if (action == m_MiddleButtonAction.action)
				button = MouseButton.Middle;
			//else if (action == m_ForwardButtonAction.action)
			//	button = MouseButton.Forward;
			//else if (action == m_BackButtonAction.action)
			//	button = MouseButton.Back;

			if (button != null) {
				var isPressed = context.control.IsPressed();
				VirtualMouse.CopyState<MouseState>(out var mouseState);
				mouseState.WithButton(button.Value, isPressed);

				InputState.Change(VirtualMouse, mouseState);
			}
		}

		// Copy-pasted from VirtualMouseInput
		private static void SetActionCallback(InputActionProperty field, Action<InputAction.CallbackContext> callback, bool install = true)
		{
			var action = field.action;
			if (action == null)
				return;

			// We don't need the performed callback as our mouse buttons are binary and thus
			// we only care about started (1) and canceled (0).

			if (install) {
				action.started += callback;
				action.canceled += callback;
			} else {
				action.started -= callback;
				action.canceled -= callback;
			}
		}
	}
}

#endif