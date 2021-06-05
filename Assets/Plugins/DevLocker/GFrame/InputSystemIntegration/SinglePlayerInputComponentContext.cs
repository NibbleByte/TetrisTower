#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with PlayerInput component.
	///
	/// IMPORTANT: never use <see cref="PlayerInput.SwitchCurrentActionMap"/> to set currently active actions directly. Use the <see cref="InputActionsStack" /> instead.
	/// </summary>
	public class SinglePlayerInputComponentContext : IInputContext
	{
		public PlayerInput PlayerInput { get; }

		public InputActionsStack InputActionsStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action PlayersChanged;


		public SinglePlayerInputComponentContext(PlayerInput playerInput, InputActionsStack inputStack)
		{
			PlayerInput = playerInput;
			InputActionsStack = inputStack;
			var uiActions = new List<InputAction>();
			if (PlayerInput.uiInputModule) {
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.point.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.leftClick.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.middleClick.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.rightClick.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.scrollWheel.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.move.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.submit.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.cancel.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDevicePosition.name));
				uiActions.Add(PlayerInput.actions.FindAction(PlayerInput.uiInputModule.trackedDeviceOrientation.name));

				uiActions.RemoveAll(a => a == null);
			}

			UIActions = uiActions;

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();
		}

		public InputAction FindActionFor(int playerIndex, string actionNameOrId, bool throwIfNotFound = false)
		{
			if (playerIndex > 0)
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

		public void ResetAllActions()
		{
			foreach (InputAction action in PlayerInput.actions) {
				action.Reset();
			}
		}
	}
}
#endif