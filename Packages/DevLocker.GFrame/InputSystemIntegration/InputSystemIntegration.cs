#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	/// Includes everything needed to display <see cref="InputBinding"/> to the UI.
	/// Any one of those fields can be null (except Binding).
	/// All texts can be localization keys if needed.
	/// </summary>
	public struct InputBindingDisplayData
	{
		/// <summary>
		/// The binding that the text / icon data is for.
		/// If this is a composite binding (e.g. 2DVector with WASD controls),
		/// texts will be summarized like "W/A/S/D" by default.
		/// You can check each part of the composite binding at the <see cref="CompositeBindingParts"/>.
		///
		/// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/api/UnityEngine.InputSystem.InputBinding.html
		/// </summary>
		public InputBinding Binding;

		/// <summary>
		/// The binding index this Binding was found at.
		/// </summary>
		public int BindingIndex;

		/// <summary>
		/// The control scheme that matched the binding.
		/// </summary>
		public string ControlScheme;

		/// <summary>
		/// The device layout that matched the binding.
		/// </summary>
		public string DeviceLayout;

		/// <summary>
		/// If the matched <see cref="Binding"/> is composite, this list stores it's part bindings.
		///
		/// NOTE: If <see cref="Binding"/> is part of composite, this collection will be null.
		/// </summary>
		public IReadOnlyList<InputBindingDisplayData> CompositeBindingParts;


		public UnityEngine.Sprite Icon;
		public string Text;
		public string ShortText;

		public bool IsValid => Binding.id != Guid.Empty;

		/// <summary>
		/// If binding is composite, it contains other bindings as part of it.
		/// In that case this binding doesn't contain actual controls to use - the sub parts do.
		/// The part bindings can be found in <see cref="CompositeBindingParts" />
		///
		/// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/api/UnityEngine.InputSystem.InputBinding.html
		/// </summary>
		public bool IsComposite => Binding.isComposite;

		public bool IsPartOfComposite => Binding.isPartOfComposite;

		public bool HasIcon => Icon != null;
		public bool HasText => !string.IsNullOrWhiteSpace(Text) || !string.IsNullOrWhiteSpace(ShortText);

		public override string ToString() => $"{Binding.name} - {ShortText}";
	}

	/// <summary>
	/// Provides the required binding representations to display hotkeys in the UI for specific device.
	/// </summary>
	public interface IInputBindingDisplayDataProvider
	{
		/// <summary>
		/// What devices does this provider has representations for.
		/// </summary>
		bool MatchesDevice(string deviceLayout);

		/// <summary>
		/// Get the display representations for the passed action.
		/// An action can have multiple bindings for the same device.
		/// </summary>
		IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(InputAction action);
	}

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
		/// Returns true if the playerIndex is the master player.
		/// This is usually the first player that has more permissions than the rest.
		/// </summary>
		bool IsMasterPlayer(int playerIndex);

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
		/// Resets all enabled actions. This will interrupt their progress and any gesture, drag, sequence will be canceled.
		/// Useful on changing states or scopes, so gestures, drags, sequences don't leak in.
		///
		/// NOTE: If you support more than one player, execute this operation for each players' action!
		/// </summary>
		void ResetAllEnabledActions();


		/// <summary>
		/// Get last updated device for specified player.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// </summary>
		InputDevice GetLastUsedInputDevice(int playerIndex);

		/// <summary>
		/// Get last used <see cref="InputControlScheme" /> for specified player.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// NOTE: If no devices used, empty control scheme will be returned.
		/// </summary>
		InputControlScheme GetLastUsedInputControlScheme(int playerIndex);

		/// <summary>
		/// Force invoke the LastUsedDeviceChanged for specified player, so UI and others can refresh.
		/// This is useful if the player changed the controls or similar,
		/// or if you're using PlayerInput component with SendMessage / Broadcast notification.
		/// Provide playerIndex with -1 to use the master player (usually the first player that has more permissions than the rest).
		/// </summary>
		void TriggerLastUsedDeviceChanged(int playerIndex = -1);

		/// <summary>
		/// Get all used <see cref="InputControlScheme" />.
		/// </summary>
		IEnumerable<InputControlScheme> GetAllInputControlSchemes();

		/// <summary>
		/// Get the display representations of the matched device for the passed action.
		/// An action can have multiple bindings for the same device.
		/// </summary>
		IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(string deviceLayout, InputAction action);
	}

	public static class InputSystemIntegrationExtensions
	{
		/// <summary>
		/// Converts enum values to int.
		/// Passing <see cref="PlayerIndex.MasterPlayer" /> will return -1.
		/// </summary>
		public static int ToIndex(this PlayerIndex playerIndex)
		{
			if (playerIndex == PlayerIndex.AnyPlayer)
				throw new ArgumentOutOfRangeException($"Trying to get int index for {playerIndex} which doesn't make sense.");

			return (int)playerIndex - (int)PlayerIndex.Player0;
		}

		/// <summary>
		/// Get the display representations of the matched device for the passed action.
		/// An action can have multiple bindings for the same device.
		/// </summary>
		public static IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(this IInputContext context, int playerIndex, InputAction action)
		{
			InputDevice lastUsedDevice = context.GetLastUsedInputDevice(playerIndex);
			if (lastUsedDevice == null) {
				return Enumerable.Empty<InputBindingDisplayData>();
			}

			return context.GetBindingDisplaysFor(lastUsedDevice.layout, action);
		}

		public static InputControlScheme GetInputControlSchemeFor(this IInputContext context, InputDevice device)
		{
			return context.GetAllInputControlSchemes().FirstOrDefault(c => c.SupportsDevice(device));
		}
	}
}
#endif