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
	/// Use this as IInputContext if you don't want to use PlayerInput component, but rather have game with simple generated IInputActionCollection class.
	/// </summary>
	public class InputCollectionContext : IInputContext
	{
		public IInputActionCollection2 InputActionsCollection { get; }

		public InputActionsMaskedStack InputActionsMaskedStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action LastUsedDeviceChanged;

		public event Action LastUsedInputControlSchemeChanged;

		public virtual bool DeviceSupportsUINavigationSelection => m_LastUsedDisplayData?.SupportsUINavigationSelection ?? false;


		private InputDevice[] m_PairedDevices = new InputDevice[0];
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

		public InputCollectionContext(IInputActionCollection2 actionsCollection, IEnumerable<InputAction> uiActions, IEnumerable<IInputBindingDisplayDataProvider> bindingDisplayProviders = null)
		{
			InputActionsCollection = actionsCollection;

			InputActionsMaskedStack = new InputActionsMaskedStack(actionsCollection);
			UIActions = new List<InputAction>(uiActions);

			m_BindingsDisplayProviders = bindingDisplayProviders != null ? bindingDisplayProviders.ToArray() : new IInputBindingDisplayDataProvider[0];

			m_LastUsedDevice = InputSystem.devices.FirstOrDefault();
			CacheDisplayData();

			if (m_LastUsedDevice != null) {
				m_LastUsedControlScheme = InputActionsCollection.controlSchemes.FirstOrDefault(c => c.SupportsDevice(m_LastUsedDevice));
			}

			// Make sure no input is enabled when starting level (including UI).
			foreach (InputAction action in InputActionsCollection) {
				action.Disable();
			}

			InputSystem.onEvent += OnInputSystemEvent;

			// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
			InputSystem.onDeviceChange += OnInputSystemDeviceChange;
		}

		public virtual void Dispose()
		{
			InputSystem.onEvent -= OnInputSystemEvent;
			InputSystem.onDeviceChange -= OnInputSystemDeviceChange;

			InputActionsMaskedStack.ForceClearAllEnableRequests();
		}

		public InputAction FindActionFor(string actionNameOrId, bool throwIfNotFound = false)
		{
			return InputActionsCollection.FindAction(actionNameOrId, throwIfNotFound);
		}

		public IEnumerable<InputAction> FindActionsForAllPlayers(string actionNameOrId, bool throwIfNotFound = false)
		{
			yield return InputActionsCollection.FindAction(actionNameOrId, throwIfNotFound);
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
			return InputActionsCollection;
		}

		public InputDevice GetLastUsedInputDevice()
		{
			return m_LastUsedDevice;
		}

		public ReadOnlyArray<InputDevice> GetPairedInputDevices()
		{
			return m_PairedDevices;
		}

		public void SetPairedInputDevices(IEnumerable<InputDevice> devices)
		{
			m_PairedDevices = devices.ToArray();
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
			return InputActionsCollection.controlSchemes;
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

		protected virtual void OnInputSystemDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (m_PairedDevices.Contains(device) || m_PairedDevices.Length == 0) {

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

			if (m_LastUsedDevice == device || (!m_PairedDevices.Contains(device) && m_PairedDevices.Length != 0))
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