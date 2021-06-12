#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.UIInputDisplay
{
	/// <summary>
	/// Displays hotkey icon / text.
	/// Refreshes if devices change.
	/// </summary>
	public class HotkeyDisplayUI : MonoBehaviour
	{
		public enum ShowPrioritySelection
		{
			IconIsPriority,
			TextIsPriority,
			ShowBoth,
		}

		[Tooltip("Which player should this hotkey be displayed for?\nIf unsure or for single player games, leave MasterPlayer.")]
		public PlayerIndex Player = PlayerIndex.MasterPlayer;

		public InputActionReference InputAction;

		[Range(0, 5)]
		[Tooltip("If multiple bindings are present in the action matching this device, display the n-th one.")]
		public int BindingNumberToUse = 0;

		[Space()]
		public ShowPrioritySelection ShowPriority = ShowPrioritySelection.IconIsPriority;

		public Image Icon;
		public Text Text;

		public bool UseShortText = true;

		private InputDevice m_LastDevice;

		/// <summary>
		/// Call this if you rebind the input or something...
		/// </summary>
		public void RefreshDisplay()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			RefreshDisplay(context, Player.ToIndex());
		}

		private void RefreshDisplay(IInputContext context, int playerIndex)
		{
			InputDevice device = context.GetLastUsedInputDevice(playerIndex);

			// HACK: Prevent from spamming on PC.
			//		 Keyboard & Mouse are be considered (usually) the same. Gamepads are not - each one comes with its own assets.
			if (device == m_LastDevice || (device is Keyboard && m_LastDevice is Mouse) || (device is Mouse && m_LastDevice is Keyboard))
				return;

			m_LastDevice = device;

			InputAction action = context.FindActionFor(playerIndex, InputAction.name);
			if (action == null) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} couldn't find specified action {InputAction.name} for player {playerIndex}", this);
				return;
			}


			int count = 0;
			var foundData = new InputBindingDisplayData();

			foreach (var bindingDisplay in context.GetBindingDisplaysFor(m_LastDevice, action)) {
				if (count == BindingNumberToUse) {
					foundData = bindingDisplay;
					break;
				}
				count++;
			}

			if (Icon) {
				bool iconIsPriority = ShowPriority == ShowPrioritySelection.IconIsPriority || ShowPriority == ShowPrioritySelection.ShowBoth || !foundData.HasText;

				Icon.gameObject.SetActive(foundData.HasIcon && iconIsPriority);
				Icon.sprite = foundData.Icon;
			}

			if (Text) {
				bool textIsPriority = ShowPriority == ShowPrioritySelection.TextIsPriority || ShowPriority == ShowPrioritySelection.ShowBoth || !foundData.HasIcon;

				string usedText = UseShortText ? foundData.ShortText : foundData.Text;
				Text.gameObject.SetActive(foundData.HasText && textIsPriority);
				Text.text = usedText;
			}
		}

		void OnEnable()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			context.LastUsedDeviceChanged += OnLastUsedDeviceChanged;
			m_LastDevice = null;
			RefreshDisplay(context, Player.ToIndex());
		}

		void OnDisable()
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			context.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;
		}

		private void OnLastUsedDeviceChanged(int playerIndex)
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (Player == PlayerIndex.MasterPlayer) {
				if (!context.IsMasterPlayer(playerIndex))
					return;
			} else if (playerIndex != Player.ToIndex()) {
				return;
			}

			RefreshDisplay(context, playerIndex);
		}

		void OnValidate()
		{
			if ((Icon && Icon.gameObject == gameObject) || (Text && Text.gameObject == gameObject)) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} has to be attached to a game object that is different from the icon / text game object. Reason: target game object will be deactivated if no binding found. Recommended: attach to the parent or panel game object.", this);
			}

			if (Player == PlayerIndex.AnyPlayer) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} doesn't allow setting {nameof(PlayerIndex.AnyPlayer)} for {nameof(Player)}.", this);
				Player = PlayerIndex.MasterPlayer;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
		}
	}

}
#endif