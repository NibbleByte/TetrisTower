#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{
	public enum PlayerIndex
	{
		AnyPlayer,		// Any Player
		MasterPlayer,   // Usually the first player that has more permissions than the rest.
		Player0,
		Player1,
		Player2,
		Player3,
		Player4,
		Player5,
		Player6,
		Player7,
		Player8,
		Player9,
		Player10,
		Player11,
		Player12,
		Player13,
		Player14,
		Player15,
	}

	/// <summary>
	/// Your <see cref="IGameContext" /> should implement this if you intend to use the Input System features of this framework,
	/// even if you're using generated IInputActionCollection.
	/// </summary>
	public interface IInputContextProvider : IDisposable
	{
		IInputContext InputContext { get; }
	}

	public delegate void PlayerIndexEventHandler(int playerIndex);

	/// <summary>
	/// Implement this if your game uses Unity Input system with generated IInputActionCollection.
	/// </summary>
	public interface IInputContext : IDisposable
	{
		/// <summary>
		/// Notifies if any player joined or left, or any other action that would require a refresh.
		/// </summary>
		event Action PlayersChanged;

		/// <summary>
		/// Last device used changed for playerIndex.
		/// </summary>
		event PlayerIndexEventHandler LastUsedDeviceChanged;


		/// <summary>
		/// Find InputAction by action name or id for specific player.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// </summary>
		InputAction FindActionFor(int playerIndex, string actionNameOrId, bool throwIfNotFound = false);

		/// <summary>
		/// Find InputActions by action name or id for all currently active players.
		/// </summary>
		IEnumerable<InputAction> FindActionsForAllPlayers(string actionNameOrId, bool throwIfNotFound = false);

		/// <summary>
		/// Push a new entry in the input actions stack, by specifying who is the source of the request.
		/// All Enable() / Disable() InputAction calls after that belong to the newly pushed (top) entry.
		/// If resetActions is true, all InputActions will be disabled after this call.
		/// Previous top entry will record the InputActions enabled flags at the moment and re-apply them when it is reactivated.
		/// It is strongly recommended to implement this method using <see cref="InputActionsStack" />.
		///
		/// NOTE: If you support more than one player, execute this operation for each players' stack!
		/// </summary>
		void PushActionsState(object source, bool resetActions = true);

		/// <summary>
		/// Removes an entry made from the specified source in the input actions stack.
		/// If that entry was the top of the stack, next entry state's enabled flags are applied to the InputActions.
		/// It is strongly recommended to implement this method using <see cref="InputActionsStack" />.
		///
		/// NOTE: If you support more than one player, execute this operation for each players' stack!
		/// </summary>
		bool PopActionsState(object source);

		/// <summary>
		/// Return all actions required for the UI input to work properly.
		/// Usually those are the ones specified in the InputSystemUIInputModule,
		/// which you can easily obtain from UnityEngine.EventSystems.EventSystem.current.currentInputModule.
		/// If you have IInputActionCollection, you can just get the InputActionMap responsible for the UI.
		/// Example: PlayerControls.UI.Get();
		///
		/// NOTE: If you support more than one player, return all players UI actions!
		/// </summary>
		IEnumerable<InputAction> GetUIActions();

		/// <summary>
		/// Resets all actions. This will interrupt their progress and any gesture, drag, sequence will be canceled.
		/// Useful on changing states or scopes, so gestures, drags, sequences don't leak in.
		///
		/// NOTE: If you support more than one player, execute this operation for each players' action!
		/// </summary>
		void ResetAllActions();


		/// <summary>
		/// Get last updated device for specified player.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// </summary>
		InputDevice GetLastUsedInputDevice(int playerIndex);

		/// <summary>
		/// Force invoke the LastUsedDeviceChanged for specified player, so UI and others can refresh.
		/// This is useful if the player changed the controls or similar,
		/// or if you're using PlayerInput component with SendMessage / Broadcast notification.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// </summary>
		void TriggerLastUsedDeviceChanged(int playerIndex = -1);
	}

	public static class PlayerIndexExtensions
	{
		public static int ToIndex(this PlayerIndex playerIndex)
		{
			if (playerIndex == PlayerIndex.AnyPlayer)
				throw new ArgumentOutOfRangeException($"Trying to get int index for {playerIndex} which doesn't make sense.");

			return (int)playerIndex - (int)PlayerIndex.Player0;
		}
	}
}
#endif