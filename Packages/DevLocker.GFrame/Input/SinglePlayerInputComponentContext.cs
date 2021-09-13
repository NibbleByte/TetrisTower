#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with PlayerInput component.
	///
	/// IMPORTANT: never use <see cref="PlayerInput.SwitchCurrentActionMap"/> to set currently active actions directly. Use the <see cref="InputActionsStack" /> instead.
	///
	/// IMPORTANT2: The LastUsedDeviceChanged event will be invoked only if you've selected the notificationBehavior to be Unity or C# events.
	///				If you prefer using messages, you'll need to trigger the TriggerLastUsedDeviceChanged() manually when devices change.
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
		public event PlayerIndexEventHandler LastUsedDeviceChanged;

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

			m_BindingsDisplayProviders = bindingDisplayProviders != null ? bindingDisplayProviders.ToArray() : new IInputBindingDisplayDataProvider[0];

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();

			// Make sure no input is enabled when starting level (including UI).
			foreach (InputAction action in PlayerInput.actions) {
				action.Disable();
			}

			// Based on the NotificationBehavior, one of these can be invoked.
			// If selected behavior is via Messages, the user have to invoke the
			// TriggerLastUsedDeviceChanged() method manually.
			PlayerInput.controlsChangedEvent.AddListener(OnControlsChanged);
			PlayerInput.onControlsChanged += OnControlsChanged;

			m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();
		}

		public void Dispose()
		{
			PlayerInput.controlsChangedEvent.RemoveListener(OnControlsChanged);
			PlayerInput.onControlsChanged -= OnControlsChanged;
		}

		public bool IsMasterPlayer(PlayerIndex playerIndex)
		{
			if (playerIndex < PlayerIndex.Player0)
				throw new ArgumentException($"{playerIndex} is not a proper player index.");

			return playerIndex == PlayerIndex.Player0;
		}

		public InputAction FindActionFor(PlayerIndex playerIndex, string actionNameOrId, bool throwIfNotFound = false)
		{
			if (playerIndex > PlayerIndex.Player0 || playerIndex == PlayerIndex.AnyPlayer)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

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

		public IEnumerable<InputAction> GetAllActionsFor(PlayerIndex playerIndex)
		{
			if (playerIndex > PlayerIndex.Player0)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			return PlayerInput.actions;
		}

		public InputDevice GetLastUsedInputDevice(PlayerIndex playerIndex)
		{
			if (playerIndex > PlayerIndex.Player0 || playerIndex == PlayerIndex.AnyPlayer)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			// HACK: In the case of keyboard and mouse, this will always return keyboard.
			return PlayerInput.devices.Count > 0
				? PlayerInput.devices[0]
				: null
				;
		}

		public InputControlScheme GetLastUsedInputControlScheme(PlayerIndex playerIndex)
		{
			if (playerIndex > PlayerIndex.Player0 || playerIndex == PlayerIndex.AnyPlayer)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			return m_LastUsedControlScheme;
		}

		public void TriggerLastUsedDeviceChanged(PlayerIndex playerIndex = PlayerIndex.MasterPlayer)
		{
			if (playerIndex > PlayerIndex.Player0 || playerIndex == PlayerIndex.AnyPlayer)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			LastUsedDeviceChanged?.Invoke(PlayerIndex.Player0);
		}

		public IEnumerable<InputControlScheme> GetAllInputControlSchemes()
		{
			foreach (PlayerInput playerInput in PlayerInput.all) {
				foreach(InputControlScheme controlScheme in playerInput.actions.controlSchemes) {
					yield return controlScheme;
				}
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

		private void OnControlsChanged(PlayerInput obj)
		{
			m_LastUsedControlScheme = PlayerInput.actions.FindControlScheme(PlayerInput.currentControlScheme) ?? new InputControlScheme();

			TriggerLastUsedDeviceChanged(PlayerIndex.Player0);
		}
	}
}
#endif