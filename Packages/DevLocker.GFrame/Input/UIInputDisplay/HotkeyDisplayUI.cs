#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Displays hotkey icon / text.
	/// Refreshes if devices change.
	/// </summary>
	public class HotkeyDisplayUI : MonoBehaviour
	{
		public enum DisplayModes
		{
			UpdateWithCurrentDevice,
			UpdateWithCurrentDeviceOnlyForControlScheme,
			UpdateWithCurrentDeviceExcludeSchemes,
			DisplaySpecificDeviceIgnoringTheCurrentOne,
		}

		[Serializable]
		public struct DisplayModeData
		{
			[Tooltip("What should the component display.")]
			public DisplayModes Mode;

			[Tooltip("Device layout name to display bindings for.")]
			public string DisplayedDeviceLayout;

			[Tooltip("When excluded, should it keep displaying the last available device bindings, or should it hide everything?")]
			public bool KeepDisplayingLastDevice;

			[InputControlSchemePicker]
			[Tooltip("Control scheme to update for. One control scheme can match multiple devices (e.g. XBox and PS gamepads). Use the picker to avoid typos.")]
			public string DisplayedControlScheme;

			[InputControlSchemePicker]
			[NonReorderable]
			[Tooltip("Control scheme to exclude. Use the picker to avoid typos.")]
			public string[] ExcludedControlSchemes;
		}

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
		[Tooltip("If multiple bindings are present in the action matching this device, display the n-th one.\n(i.e. \"alternative binding\")")]
		public int BindingNumberToUse = 0;

		[Range(0, 6)]
		[Tooltip("If matched binding is composite (consists of multiple parts, e.g. WASD or Arrows), display the n-th part of it instead.\nIf value is left 0, it will display the initial composite binding summarized (e.g. \"W/A/S/D\")")]
		public int CompositePartNumberToUse = 0;

		public DisplayModeData DisplayMode;

		[Space()]
		[Tooltip("Should it show icon or text if available. If not it will display whatever it can.")]
		public ShowPrioritySelection ShowPriority = ShowPrioritySelection.IconIsPriority;

		public Image Icon;
		public Text Text;

		public bool UseShortText = true;

		[Tooltip("Optional - enter how the hotkey text should be displayed. Use \"{Hotkey}\" to be replaced with the matched text.\nLeave empty to skip.")]
		public string FormatText;

		public InputBindingDisplayData CurrentlyDisplayedData { get; private set; }

		private InputDevice m_LastDevice;

		/// <summary>
		/// Call this if you rebind the input or something...
		/// </summary>
		public void RefreshDisplay()
		{
			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_LastDevice = null;
			RefreshDisplay(InputContextManager.InputContext, Player);
		}

		private void RefreshDisplay(IInputContext context, PlayerIndex playerIndex)
		{
			string deviceLayout;

			if (DisplayMode.Mode != DisplayModes.DisplaySpecificDeviceIgnoringTheCurrentOne) {
				InputDevice device = context.GetLastUsedInputDevice(playerIndex);

				// HACK: Prevent from spamming on PC.
				//		 Keyboard & Mouse are be considered (usually) the same. Gamepads are not - each one comes with its own assets.
				if ((device is Keyboard && m_LastDevice is Mouse) || (device is Mouse && m_LastDevice is Keyboard))
					return;

				m_LastDevice = device;

				if (DisplayMode.Mode == DisplayModes.UpdateWithCurrentDeviceOnlyForControlScheme) {
					var lastControlScheme = context.GetLastUsedInputControlScheme(playerIndex).bindingGroup;

					if (!DisplayMode.DisplayedControlScheme.Equals(lastControlScheme, StringComparison.OrdinalIgnoreCase)) {

						if (!DisplayMode.KeepDisplayingLastDevice) {
							if (Icon) Icon.gameObject.SetActive(false);
							if (Text) Text.gameObject.SetActive(false);
							return;
						}

						// I'm displayed for the first time - display first available device if current device is to be excluded.
						m_LastDevice = InputSystem.devices
							.FirstOrDefault(d => DisplayMode.DisplayedControlScheme.Equals(context.GetInputControlSchemeFor(d).bindingGroup))
							;
					}
				}

				if (DisplayMode.Mode == DisplayModes.UpdateWithCurrentDeviceExcludeSchemes) {
					var lastControlScheme = context.GetLastUsedInputControlScheme(playerIndex).bindingGroup;

					if (DisplayMode.ExcludedControlSchemes.Contains(lastControlScheme)) {

						if (!DisplayMode.KeepDisplayingLastDevice) {
							if (Icon) Icon.gameObject.SetActive(false);
							if (Text) Text.gameObject.SetActive(false);
							return;
						}

						// I'm displayed for the first time - display first available device if current device is to be excluded.
						m_LastDevice = InputSystem.devices
							.FirstOrDefault(d => !DisplayMode.ExcludedControlSchemes.Contains(context.GetInputControlSchemeFor(d).bindingGroup))
							;
					}
				}

				// Player may not have available devices at the moment. Abort in that case.
				if (m_LastDevice == null)
					return;

				deviceLayout = m_LastDevice.layout;

			} else {
				deviceLayout = DisplayMode.DisplayedDeviceLayout;
			}

			InputAction action = context.FindActionFor(playerIndex, InputAction.name);
			if (action == null) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} couldn't find specified action {InputAction.name} for player {playerIndex}", this);
				return;
			}

			int count = 0;
			var displayedData = new InputBindingDisplayData();

			foreach (var bindingDisplay in context.GetBindingDisplaysFor(deviceLayout, action)) {
				if (count == BindingNumberToUse) {

					if (CompositePartNumberToUse == 0) {
						displayedData = bindingDisplay;
					} else if (CompositePartNumberToUse - 1 < bindingDisplay.CompositeBindingParts.Count) {
						displayedData = bindingDisplay.CompositeBindingParts[CompositePartNumberToUse - 1];
					}

					break;
				}
				count++;
			}

			displayedData.DeviceLayout = deviceLayout;
			CurrentlyDisplayedData = displayedData;

			if (Icon) {
				bool iconIsPriority = ShowPriority == ShowPrioritySelection.IconIsPriority || ShowPriority == ShowPrioritySelection.ShowBoth || !CurrentlyDisplayedData.HasText;

				Icon.gameObject.SetActive(CurrentlyDisplayedData.HasIcon && iconIsPriority);
				Icon.sprite = CurrentlyDisplayedData.Icon;
			}

			if (Text) {
				bool textIsPriority = ShowPriority == ShowPrioritySelection.TextIsPriority || ShowPriority == ShowPrioritySelection.ShowBoth || !CurrentlyDisplayedData.HasIcon;

				string usedText = UseShortText ? CurrentlyDisplayedData.ShortText : CurrentlyDisplayedData.Text;
				Text.gameObject.SetActive(CurrentlyDisplayedData.HasText && textIsPriority);
				Text.text = string.IsNullOrWhiteSpace(FormatText)
					? usedText
					: FormatText.Replace("{Hotkey}", usedText)
					;
			}
		}

		void OnEnable()
		{

			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			InputContextManager.InputContext.LastUsedDeviceChanged += OnLastUsedDeviceChanged;
			m_LastDevice = null;
			RefreshDisplay(InputContextManager.InputContext, Player);
		}

		void OnDisable()
		{
			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			InputContextManager.InputContext.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;

			if (Icon) {
				Icon.gameObject.SetActive(false);
				Icon.sprite = null;
			}

			if (Text) {
				Text.gameObject.SetActive(false);
			}
		}

		private void OnLastUsedDeviceChanged(PlayerIndex playerIndex)
		{
			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyDisplayUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (Player == PlayerIndex.MasterPlayer) {
				if (!InputContextManager.InputContext.IsMasterPlayer(playerIndex))
					return;
			} else if (playerIndex != Player) {
				return;
			}

			RefreshDisplay(InputContextManager.InputContext, playerIndex);
		}

		void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, InputAction, nameof(InputAction));
			Utils.Validation.ValidateMissingObject(this, Icon, nameof(Icon));
			Utils.Validation.ValidateMissingObject(this, Text, nameof(Text));

			if ((Icon && Icon.gameObject == gameObject) || (Text && Text.gameObject == gameObject)) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} {name} has to be attached to a game object that is different from the icon / text game object. Reason: target game object will be deactivated if no binding found. Recommended: attach to the parent or panel game object.", this);
			}

			if (Player == PlayerIndex.AnyPlayer) {
				Debug.LogError($"{nameof(HotkeyDisplayUI)} {name} doesn't allow setting {nameof(PlayerIndex.AnyPlayer)} for {nameof(Player)}.", this);
				Player = PlayerIndex.MasterPlayer;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(HotkeyDisplayUI.DisplayModeData))]
	internal class HotkeyDisplayUIUpdateModeDataPropertyDrawer : UnityEditor.PropertyDrawer
	{
		public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
		{
			var modeProperty = property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.Mode));
			var mode = (HotkeyDisplayUI.DisplayModes)modeProperty.enumValueIndex;

			float height = UnityEditor.EditorGUIUtility.singleLineHeight;

			switch(mode) {
				case HotkeyDisplayUI.DisplayModes.DisplaySpecificDeviceIgnoringTheCurrentOne:
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedDeviceLayout)), label);
					break;
				case HotkeyDisplayUI.DisplayModes.UpdateWithCurrentDeviceOnlyForControlScheme:
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.KeepDisplayingLastDevice)), label);
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedControlScheme)), label);
					break;
				case HotkeyDisplayUI.DisplayModes.UpdateWithCurrentDeviceExcludeSchemes:
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.KeepDisplayingLastDevice)), label);
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.ExcludedControlSchemes)), label);
					break;
			}

			return height;
		}

		public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
		{
			label = UnityEditor.EditorGUI.BeginProperty(position, label, property);

			Rect lineRect = position;
			lineRect.height = UnityEditor.EditorGUIUtility.singleLineHeight;

			var modeProperty = property.FindPropertyRelative("Mode");
			UnityEditor.EditorGUI.PropertyField(lineRect, modeProperty, label);

			lineRect.y += UnityEditor.EditorGUIUtility.singleLineHeight + UnityEditor.EditorGUIUtility.standardVerticalSpacing;

			var mode = (HotkeyDisplayUI.DisplayModes) modeProperty.enumValueIndex;
			UnityEditor.EditorGUI.indentLevel++;
			switch(mode) {
				case HotkeyDisplayUI.DisplayModes.UpdateWithCurrentDevice:
					if (property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedDeviceLayout)).stringValue != string.Empty) {
						property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedDeviceLayout)).stringValue = string.Empty;
					}
					if (property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.ExcludedControlSchemes)).arraySize != 0) {
						property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.ExcludedControlSchemes)).arraySize = 0;
					}
					break;

				case HotkeyDisplayUI.DisplayModes.DisplaySpecificDeviceIgnoringTheCurrentOne:
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedDeviceLayout)));
					break;
				case HotkeyDisplayUI.DisplayModes.UpdateWithCurrentDeviceOnlyForControlScheme:
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.KeepDisplayingLastDevice)), true);
					lineRect.y += UnityEditor.EditorGUIUtility.singleLineHeight + UnityEditor.EditorGUIUtility.standardVerticalSpacing;
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedControlScheme)), true);
					break;
				case HotkeyDisplayUI.DisplayModes.UpdateWithCurrentDeviceExcludeSchemes:
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.KeepDisplayingLastDevice)), true);
					lineRect.y += UnityEditor.EditorGUIUtility.singleLineHeight + UnityEditor.EditorGUIUtility.standardVerticalSpacing;
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.ExcludedControlSchemes)), true);
					break;
			}
			UnityEditor.EditorGUI.indentLevel--;

			UnityEditor.EditorGUI.EndProperty();
		}
	}
#endif

}
#endif