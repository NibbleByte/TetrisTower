#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace DevLocker.GFrame.Input.Contexts
{

	/// <summary>
	/// Use this as IInputContext if you want to use the PlayerInput component in your game.
	///
	/// IMPORTANT: never use <see cref="PlayerInput.SwitchCurrentActionMap"/> to set currently active actions directly. Use the <see cref="InputActionsStack" /> instead.
	/// </summary>
	public class InputComponentContext : IInputContext
	{
		public PlayerInput PlayerInput { get; }

		public InputActionsMaskedStack InputActionsMaskedStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		/// <summary>
		/// IMPORTANT2: The LastUsedDeviceChanged event will be invoked only if you've selected the notificationBehavior to be Unity or C# events.
		///				If you prefer using messages, you'll need to trigger the TriggerLastUsedDeviceChanged() manually when devices change.
		/// </summary>
		public event Action LastUsedDeviceChanged;
		public event Action LastUsedInputControlSchemeChanged;

		public virtual bool DeviceSupportsUINavigationSelection => m_LastUsedDisplayData?.SupportsUINavigationSelection ?? false;

		private InputDevice m_LastUsedDevice;
		private InputControlScheme m_LastUsedControlScheme;

		private IInputBindingDisplayDataProvider m_LastUsedDisplayData;
		private readonly IInputBindingDisplayDataProvider[] m_BindingsDisplayProviders;

		private InputDevice m_ForcedDevice;
		public InputDevice ForcedDevice {
			get => m_ForcedDevice;
			set {
				if (m_ForcedDevice == value)
					return;

				m_ForcedDevice = value;
				if (m_ForcedDevice != null) {
					m_LastUsedDevice = m_ForcedDevice;
					CacheDisplayData();
					m_LastUsedControlScheme = this.GetInputControlSchemeFor(m_LastUsedDevice);

					TriggerLastUsedInputControlSchemeChanged();
					TriggerLastUsedDeviceChanged();
				}
			}
		}

		public InputComponentContext(PlayerInput playerInput, InputActionsMaskedStack inputStack, IEnumerable<IInputBindingDisplayDataProvider> bindingDisplayProviders = null)
		{
			PlayerInput = playerInput;

			// We'll control the input actions.
			PlayerInput.defaultActionMap = "";

			InputActionsMaskedStack = inputStack;
			var uiActions = new List<InputAction>();
			if (PlayerInput.uiInputModule) {
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.point?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.leftClick?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.middleClick?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.rightClick?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.scrollWheel?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.move?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.submit?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.cancel?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDevicePosition?.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDeviceOrientation?.action?.id ?? new Guid()));

				uiActions.RemoveAll(a => a == null);
			}

			UIActions = uiActions;

			m_BindingsDisplayProviders = bindingDisplayProviders != null ? bindingDisplayProviders.ToArray() : new IInputBindingDisplayDataProvider[0];

			if (PlayerInput.currentControlScheme != null) {
				m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();
				m_LastUsedDevice = InputSystem.devices.FirstOrDefault(d => m_LastUsedControlScheme.SupportsDevice(d));
				CacheDisplayData();
			}

			// Make sure no input is enabled when starting level (including UI).
			foreach (InputAction action in PlayerInput.actions) {
				action.Disable();
			}

			// NOTE: Not used as it is hard to get the last used device (can't distinguish between keyboard and a mouse).
			// Based on the NotificationBehavior, one of these can be invoked.
			// If selected behavior is via Messages, the user have to invoke the
			// TriggerLastUsedDeviceChanged() method manually.
			//PlayerInput.controlsChangedEvent.AddListener(OnControlsChanged);
			//PlayerInput.onControlsChanged += OnControlsChanged;

			/// From the summary of the class, but not used anymore.
			/// IMPORTANT2: The <see cref="LastUsedDeviceChanged"/> event will be invoked only if you've selected the notificationBehavior to be Unity or C# events.
			///				If you prefer using messages, you'll need to trigger the <see cref="TriggerLastUsedDeviceChanged"/>() manually when devices change.

			InputSystem.onEvent += OnInputSystemEvent;

			// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
			InputSystem.onDeviceChange += OnInputSystemDeviceChange;
		}

		public virtual void Dispose()
		{
			//PlayerInput.controlsChangedEvent.RemoveListener(OnControlsChanged);
			//PlayerInput.onControlsChanged -= OnControlsChanged;

			InputSystem.onEvent -= OnInputSystemEvent;
			InputSystem.onDeviceChange -= OnInputSystemDeviceChange;

			InputActionsMaskedStack.ForceClearAllEnableRequests();
		}

		public InputAction FindActionFor(string actionNameOrId, bool throwIfNotFound = false)
		{
			return PlayerInput ? PlayerInput.actions.FindAction(actionNameOrId, throwIfNotFound) : null;
		}

		public IEnumerable<InputAction> FindActionsForAllPlayers(string actionNameOrId, bool throwIfNotFound = false)
		{
			if (PlayerInput == null)
				yield break;

			yield return PlayerInput.actions.FindAction(actionNameOrId, throwIfNotFound);
		}

		public void EnableAction(object source, InputAction action)
		{
			InputActionsMaskedStack.Enable(source, action);
		}

		public void DisableAction(object source, InputAction action)
		{
			InputActionsMaskedStack.Disable(source, action);
		}

		public void DisableAll(object source)
		{
			InputActionsMaskedStack.Disable(source);
		}

		public IEnumerable<InputAction> GetInputActionsEnabledBy(object source)
		{
			return InputActionsMaskedStack.GetInputActionsEnabledBy(source);
		}

		public IEnumerable<object> GetEnablingSourcesFor(InputAction action)
		{
			return InputActionsMaskedStack.GetEnablingSourcesFor(action);
		}

		public bool IsEnabledBy(InputAction action, object source)
		{
			return InputActionsMaskedStack.IsEnabledBy(action, source);
		}

		public void PushOrSetActionsMask(object source, IEnumerable<InputAction> actionsMask, bool setBackToTop = false)
		{
			InputActionsMaskedStack.PushOrSetActionsMask(source, actionsMask, setBackToTop);
		}

		public void PopActionsMask(object source)
		{
			InputActionsMaskedStack.PopActionsMask(source);
		}

		public IEnumerable<InputAction> GetUIActions()
		{
			return UIActions;
		}

		public IEnumerable<InputAction> GetAllActions()
		{
			return PlayerInput ? PlayerInput.actions : Enumerable.Empty<InputAction>();
		}

		public InputDevice GetLastUsedInputDevice()
		{
			return m_LastUsedDevice;
		}

		public ReadOnlyArray<InputDevice> GetPairedInputDevices()
		{
			return PlayerInput ? PlayerInput.devices : new InputDevice[0];
		}

		public InputControlScheme GetLastUsedInputControlScheme()
		{
			return m_LastUsedControlScheme;
		}

		public virtual void TriggerLastUsedDeviceChanged()
		{
			LastUsedDeviceChanged?.Invoke();
		}

		public virtual void TriggerLastUsedInputControlSchemeChanged()
		{
			LastUsedInputControlSchemeChanged?.Invoke();
		}

		public IEnumerable<InputControlScheme> GetAllInputControlSchemes()
		{
			if (PlayerInput == null)
				yield break;

			foreach(InputControlScheme controlScheme in PlayerInput.actions.controlSchemes) {
				yield return controlScheme;
			}
		}

		public IReadOnlyList<IInputBindingDisplayDataProvider> GetAllDisplayDataProviders()
		{
			return m_BindingsDisplayProviders;
		}

		public IInputBindingDisplayDataProvider GetCurrentDisplayDataProvider()
		{
			return m_LastUsedDisplayData;
		}

		private void CacheDisplayData()
		{
			if (m_LastUsedDevice == null) {
				m_LastUsedDisplayData = null;
				return;
			}

			foreach (var displayData in m_BindingsDisplayProviders) {
				if (displayData.MatchesDevice(m_LastUsedDevice.layout)) {
					m_LastUsedDisplayData = displayData;
					return;
				}
			}

			m_LastUsedDisplayData = null;
		}

		// NOTE: Not used anymore, check the init method.
		//private void OnControlsChanged(PlayerInput obj)
		//{
		//	InputControlScheme prevScheme = m_LastUsedControlScheme;
		//	m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();
		//
		//	if (m_LastUsedControlScheme != prevScheme) {
		//		TriggerLastUsedInputControlSchemeChanged();
		//	}
		//
		//	TriggerLastUsedDeviceChanged();
		//}

		protected virtual void OnInputSystemDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (PlayerInput == null)
				return;

			if (PlayerInput.devices.Contains(device)) {

				// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
				// Trigger event so UI gets refreshed properly.
				TriggerLastUsedDeviceChanged();
			}
		}

		protected virtual void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
		{
			// Faking it.
			if (m_ForcedDevice != null)
				return;

			if (m_LastUsedDevice == device || PlayerInput == null || !PlayerInput.devices.Contains(device))
				return;

			// Some devices like to spam events like crazy.
			// Example: PS4 controller on PC keeps triggering events without meaningful change.
			var eventType = eventPtr.type;
			if (eventType == StateEvent.Type) {

				// Go through the changed controls in the event and look for ones actuated
				// above a magnitude of a little above zero.
				if (!eventPtr.EnumerateChangedControls(device: device, magnitudeThreshold: 0.0001f).Any())
					return;
			}

			m_LastUsedDevice = device;
			CacheDisplayData();

			if (m_LastUsedDevice != null) {
				InputControlScheme prevScheme = m_LastUsedControlScheme;
				m_LastUsedControlScheme = this.GetInputControlSchemeFor(m_LastUsedDevice);

				if (m_LastUsedControlScheme != prevScheme) {
					TriggerLastUsedInputControlSchemeChanged();
				}
			}

			TriggerLastUsedDeviceChanged();
		}
	}
}
#endif