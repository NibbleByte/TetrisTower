#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DevLocker.GFrame.Input
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with PlayerInput component.
	///
	/// IMPORTANT: never use <see cref="PlayerInput.SwitchCurrentActionMap"/> to set currently active actions directly. Use the <see cref="InputActionsStack" /> instead.
	///
	/// IMPORTANT2: The <see cref="LastUsedDeviceChanged"/> event will be invoked only if you've selected the notificationBehavior to be Unity or C# events.
	///				If you prefer using messages, you'll need to trigger the <see cref="TriggerLastUsedDeviceChanged"/>() manually when devices change.
	/// </summary>
	public sealed class SinglePlayerInputComponentContext : IInputContext
	{
		public PlayerInput PlayerInput { get; }

		public InputActionsStack InputActionsStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action PlayersChanged;

		/// <summary>
		/// IMPORTANT2: The LastUsedDeviceChanged event will be invoked only if you've selected the notificationBehavior to be Unity or C# events.
		///				If you prefer using messages, you'll need to trigger the TriggerLastUsedDeviceChanged() manually when devices change.
		/// </summary>
		public event Action LastUsedDeviceChanged;

		private InputDevice m_LastUsedDevice;
		private InputControlScheme m_LastUsedControlScheme;

		private readonly IInputBindingDisplayDataProvider[] m_BindingsDisplayProviders;

		public SinglePlayerInputComponentContext(PlayerInput playerInput, InputActionsStack inputStack, IEnumerable<IInputBindingDisplayDataProvider> bindingDisplayProviders = null)
		{
			PlayerInput = playerInput;

			// We'll control the input actions.
			PlayerInput.defaultActionMap = "";

			InputActionsStack = inputStack;
			var uiActions = new List<InputAction>();
			if (PlayerInput.uiInputModule) {
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.point.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.leftClick.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.middleClick.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.rightClick.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.scrollWheel.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.move.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.submit.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.cancel.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDevicePosition.action?.id ?? new Guid()));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDeviceOrientation.action?.id ?? new Guid()));

				uiActions.RemoveAll(a => a == null);
			}

			UIActions = uiActions;

			m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();
			m_LastUsedDevice = InputSystem.devices.FirstOrDefault(d => m_LastUsedControlScheme.SupportsDevice(d));

			m_BindingsDisplayProviders = bindingDisplayProviders != null ? bindingDisplayProviders.ToArray() : new IInputBindingDisplayDataProvider[0];

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();

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

			InputSystem.onEvent += OnInputSystemEvent;

			// Called when device configuration changes (for example keyboard layout / language), not on switching devices.
			InputSystem.onDeviceChange += OnInputSystemDeviceChange;
		}

		public void Dispose()
		{
			//PlayerInput.controlsChangedEvent.RemoveListener(OnControlsChanged);
			//PlayerInput.onControlsChanged -= OnControlsChanged;

			InputSystem.onEvent -= OnInputSystemEvent;
			InputSystem.onDeviceChange -= OnInputSystemDeviceChange;
		}

		public InputAction FindActionFor(string actionNameOrId, bool throwIfNotFound = false)
		{
			return PlayerInput.actions.FindAction(actionNameOrId, throwIfNotFound);
		}

		public IEnumerable<InputAction> FindActionsForAllPlayers(string actionNameOrId, bool throwIfNotFound = false)
		{
			yield return PlayerInput.actions.FindAction(actionNameOrId, throwIfNotFound);
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
			return PlayerInput.actions;
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
			foreach(InputControlScheme controlScheme in PlayerInput.actions.controlSchemes) {
				yield return controlScheme;
			}
		}

		public IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(string deviceLayout, InputAction action)
		{
			foreach (var displaysProvider in m_BindingsDisplayProviders) {
				if (displaysProvider.MatchesDevice(deviceLayout)) {
					foreach (var bindingDisplay in displaysProvider.GetBindingDisplaysFor(action)) {
						yield return bindingDisplay;
					}
				}
			}
		}

		// NOTE: Not used anymore, check the init method.
		//private void OnControlsChanged(PlayerInput obj)
		//{
		//	m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();
		//
		//	TriggerLastUsedDeviceChanged(PlayerIndex.Player0);
		//}

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