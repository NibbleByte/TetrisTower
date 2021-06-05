#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{

	/// <summary>
	/// Use this as IInputContext if you have a single player game with generated IInputActionCollection class.
	/// </summary>
	public class SinglePlayerInputCollectionContext : IInputContext
	{
		public IInputActionCollection2 InputActionsCollection { get; }

		public InputActionsStack InputActionsStack { get; }

		public IReadOnlyCollection<InputAction> UIActions { get; }

		public event Action PlayersChanged;


		public SinglePlayerInputCollectionContext(IInputActionCollection2 actionsCollection, InputActionsStack inputStack, IEnumerable<InputAction> uiActions)
		{
			InputActionsCollection = actionsCollection;
			InputActionsStack = inputStack;
			UIActions = new List<InputAction>(uiActions);

			// HACK: To silence warning that it is never used.
			PlayersChanged?.Invoke();
		}

		public InputAction FindActionFor(int playerIndex, string actionNameOrId, bool throwIfNotFound = false)
		{
			if (playerIndex > 0)
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
	}
}
#endif