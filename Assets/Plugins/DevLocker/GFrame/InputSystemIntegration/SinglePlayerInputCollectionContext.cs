#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with generated IInputActionCollection class.
	/// </summary>
	public sealed class SinglePlayerInputCollectionContext : IInputContext
	{
		public IInputActionCollection2 InputActionsCollection { get; }

		public InputActionsStack InputActionsStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action PlayersChanged;
		public event PlayerIndexEventHandler LastUsedDeviceChanged;

		private InputDevice m_LastUsedDevice;

		public SinglePlayerInputCollectionContext(IInputActionCollection2 actionsCollection, InputActionsStack inputStack, IEnumerable<InputAction> uiActions)
		{
			InputActionsCollection = actionsCollection;
			InputActionsStack = inputStack;
			UIActions = new List<InputAction>(uiActions);

			m_LastUsedDevice = InputSystem.devices.FirstOrDefault();

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();

			InputSystem.onEvent += OnInputSystemEvent;
		}

		public void Dispose()
		{
			InputSystem.onEvent -= OnInputSystemEvent;
		}

		public InputAction FindActionFor(int playerIndex, string actionNameOrId, bool throwIfNotFound = false)
		{
			if (playerIndex > 0 || playerIndex < -1)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

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

		public void ResetAllActions()
		{
			foreach (InputAction action in InputActionsCollection) {
				action.Reset();
			}
		}


		public InputDevice GetLastUsedInputDevice(int playerIndex)
		{
			if (playerIndex > 0 || playerIndex < -1)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			return m_LastUsedDevice;
		}

		public void TriggerLastUsedDeviceChanged(int playerIndex = -1)
		{
			if (playerIndex > 0 || playerIndex < -1)
				throw new NotSupportedException($"Only single player is supported, but {playerIndex} was requested.");

			LastUsedDeviceChanged?.Invoke(0);
		}

		private void OnInputSystemEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, InputDevice device)
		{
			if (m_LastUsedDevice == device)
				return;

			m_LastUsedDevice = device;

			TriggerLastUsedDeviceChanged(0);
		}
	}
}
#endif