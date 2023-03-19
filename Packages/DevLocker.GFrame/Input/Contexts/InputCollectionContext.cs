#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DevLocker.GFrame.Input.Contexts
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with generated IInputActionCollection class.
	/// </summary>
	public sealed class InputCollectionContext : IInputContext
	{
		public IInputActionCollection2 InputActionsCollection { get; }

		public InputActionsStack InputActionsStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action PlayersChanged;
		public event Action LastUsedDeviceChanged;


		private InputDevice m_LastUsedDevice;
		private InputControlScheme m_LastUsedControlScheme;
		private readonly IInputBindingDisplayDataProvider[] m_BindingsDisplayProviders;

		public InputCollectionContext(IInputActionCollection2 actionsCollection, InputActionsStack inputStack, IEnumerable<InputAction> uiActions, IEnumerable<IInputBindingDisplayDataProvider> bindingDisplayProviders = null)
		{
			InputActionsCollection = actionsCollection;

			InputActionsStack = inputStack;
			UIActions = new List<InputAction>(uiActions);

			m_LastUsedDevice = InputSystem.devices.FirstOrDefault();
			if (m_LastUsedDevice != null) {
				m_LastUsedControlScheme = InputActionsCollection.controlSchemes.FirstOrDefault(c => c.SupportsDevice(m_LastUsedDevice));
			}

			m_BindingsDisplayProviders = bindingDisplayProviders != null ? bindingDisplayProviders.ToArray() : new IInputBindingDisplayDataProvider[0];

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();

			// Make sure no input is enabled when starting level (including UI).
			foreach (InputAction action in InputActionsCollection) {
				action.Disable();
			}

			InputSystem.onEvent += OnInputSystemEvent;

			// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
			InputSystem.onDeviceChange += OnInputSystemDeviceChange;
		}

		public void Dispose()
		{
			InputSystem.onEvent -= OnInputSystemEvent;
			InputSystem.onDeviceChange -= OnInputSystemDeviceChange;
		}

		public InputAction FindActionFor(string actionNameOrId, bool throwIfNotFound = false)
		{
			return InputActionsCollection.FindAction(actionNameOrId, throwIfNotFound);
		}

		public IEnumerable<InputAction> FindActionsForAllPlayers(string actionNameOrId, bool throwIfNotFound = false)
		{
			yield return InputActionsCollection.FindAction(actionNameOrId, throwIfNotFound);
		}

		public void PushActionsState(object source, bool resetActions = true)
		{
			InputActionsStack.PushActionsState(source, resetActions);
		}

		public bool PopActionsState(object source)
		{
			return InputActionsStack.PopActionsState(source);
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

		public InputControlScheme GetLastUsedInputControlScheme()
		{
			return m_LastUsedControlScheme;
		}

		public void TriggerLastUsedDeviceChanged()
		{
			LastUsedDeviceChanged?.Invoke();
		}

		public IEnumerable<InputControlScheme> GetAllInputControlSchemes()
		{
			return InputActionsCollection.controlSchemes;
		}

		public IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(string deviceLayout, InputAction action)
		{
			foreach (var displaysProvider in m_BindingsDisplayProviders) {
				if (displaysProvider.MatchesDevice(deviceLayout)) {
					foreach(var bindingDisplay in displaysProvider.GetBindingDisplaysFor(action)) {
						yield return bindingDisplay;
					}
				}
			}
		}

		private void OnInputSystemDeviceChange(InputDevice device, InputDeviceChange change)
		{
			// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
			// Trigger event so UI gets refreshed properly.
			TriggerLastUsedDeviceChanged();
		}

		private void OnInputSystemEvent(InputEventPtr eventPtr, InputDevice device)
		{
			if (m_LastUsedDevice == device)
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
			if (m_LastUsedDevice != null) {
				m_LastUsedControlScheme = this.GetInputControlSchemeFor(m_LastUsedDevice);
			}

			TriggerLastUsedDeviceChanged();
		}
	}
}
#endif