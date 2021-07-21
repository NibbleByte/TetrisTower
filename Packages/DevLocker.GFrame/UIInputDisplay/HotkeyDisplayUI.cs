#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
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
		public enum DisplayModes
		{
			UpdateWithCurrentDevice,
			DisplaySpecificDeviceIgnoringTheCurrentOne,
			UpdateWithCurrentDeviceExcludeSchemes,
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

			m_LastDevice = null;
			RefreshDisplay(context, Player.ToIndex());
		}

		private void RefreshDisplay(IInputContext context, int playerIndex)
		{
			string deviceLayout;

			if (DisplayMode.Mode != DisplayModes.DisplaySpecificDeviceIgnoringTheCurrentOne) {
				InputDevice device = context.GetLastUsedInputDevice(playerIndex);

				// HACK: Prevent from spamming on PC.
				//		 Keyboard & Mouse are be considered (usually) the same. Gamepads are not - each one comes with its own assets.
				if (device == m_LastDevice || (device is Keyboard && m_LastDevice is Mouse) || (device is Mouse && m_LastDevice is Keyboard))
					return;

				bool hadDeviceBefore = m_LastDevice != null;
				m_LastDevice = device;

				if (DisplayMode.Mode == DisplayModes.UpdateWithCurrentDeviceExcludeSchemes) {
					var lastControlScheme = context.GetLastUsedInputControlScheme(playerIndex).bindingGroup;

					if (DisplayMode.ExcludedControlSchemes.Contains(lastControlScheme)) {

						if (!DisplayMode.KeepDisplayingLastDevice) {
							if (Icon) Icon.gameObject.SetActive(false);
							if (Text) Text.gameObject.SetActive(false);
							return;
						}

						if (hadDeviceBefore)
							return;

						// I'm displayed for the first time - display first available device if current device is to be excluded.
						m_LastDevice = InputSystem.devices
							.Where(d => d != device)
							.FirstOrDefault(d => !DisplayMode.ExcludedControlSchemes.Contains(context.GetInputControlSchemeFor(d).bindingGroup))
							;

						if (m_LastDevice == null)
							return;
					}
				}

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
			var foundData = new InputBindingDisplayData();

			foreach (var bindingDisplay in context.GetBindingDisplaysFor(deviceLayout, action)) {
				if (count == BindingNumberToUse) {

					if (CompositePartNumberToUse == 0) {
						foundData = bindingDisplay;
					} else if (CompositePartNumberToUse - 1 < bindingDisplay.CompositeBindingParts.Count) {
						foundData = bindingDisplay.CompositeBindingParts[CompositePartNumberToUse - 1];
					}

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
				Text.text = string.IsNullOrWhiteSpace(FormatText)
					? usedText
					: FormatText.Replace("{Hotkey}", usedText)
					;
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

			if (Icon) {
				Icon.gameObject.SetActive(false);
				Icon.sprite = null;
			}

			if (Text) {
				Text.gameObject.SetActive(false);
			}
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
			var modeProperty = property.FindPropertyRelative("Mode");
			var mode = (HotkeyDisplayUI.DisplayModes)modeProperty.enumValueIndex;

			float height = UnityEditor.EditorGUIUtility.singleLineHeight;

			switch(mode) {
				case HotkeyDisplayUI.DisplayModes.DisplaySpecificDeviceIgnoringTheCurrentOne:
					height += UnityEditor.EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(HotkeyDisplayUI.DisplayModeData.DisplayedDeviceLayout)), label);
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