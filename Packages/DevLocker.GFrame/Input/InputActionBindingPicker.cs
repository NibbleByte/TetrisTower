#if USE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Displays both input action and binding.
	/// Stores the binding guid as string.
	///
	/// You should rarely use this as current control scheme may change and this class locks you to the binding's one.
	/// </summary>
	[Serializable]
	public class InputActionBindingPair
	{
		public InputActionReference InputAction;

		public string BindingId;

		public InputBinding GetBinding() {
			var action = InputAction?.action;
			if (action == null)
				return default;

			foreach(InputBinding binding in action.bindings) {
				if (binding.id.ToString().Equals(BindingId)) {
					return binding;
				}
			}

			return default;
		}

		public bool IsBindingBroken() {
			var action = InputAction?.action;
			if (action == null)
				return false;

			foreach(InputBinding binding in action.bindings) {
				if (binding.id.ToString().Equals(BindingId)) {
					return false;
				}
			}

			return true;
		}

		public bool IsValid()
		{
			var action = InputAction?.action;
			if (action == null)
				return false;

			if (string.IsNullOrWhiteSpace(BindingId))
				return false;

			return !IsBindingBroken();
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Drawer that displays available bindings for the selected action.
	/// NOTE: This class is mostly copied from the RebindActionUIEditor in the "Rebinding UI" sample
	/// </summary>
	[CustomPropertyDrawer(typeof(InputActionBindingPair))]
	internal class InputActionBindingPickerPropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight * 2;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			//EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			SerializedProperty actionProperty = property.FindPropertyRelative("InputAction");
			SerializedProperty bindingIdProperty = property.FindPropertyRelative("BindingId");

			var actionReference = (InputActionReference)actionProperty.objectReferenceValue;
			var action = actionReference?.action;
			GUIContent[] bindingOptions = new GUIContent[0];
			string[] bindingOptionValues = new string[0];
			int selectedBindingOption = -1;

			if (action != null) {

				var bindings = action.bindings;
				var bindingCount = bindings.Count;

				bindingOptions = new GUIContent[bindingCount];
				bindingOptionValues = new string[bindingCount];
				selectedBindingOption = -1;

				var currentBindingId = bindingIdProperty.stringValue;
				for (var i = 0; i < bindingCount; ++i) {
					var binding = bindings[i];
					var bindingId = binding.id.ToString();
					var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

					// If we don't have a binding groups (control schemes), show the device that if there are, for example,
					// there are two bindings with the display string "A", the user can see that one is for the keyboard
					// and the other for the gamepad.
					var displayOptions =
						InputBinding.DisplayStringOptions.DontUseShortDisplayNames | InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
					if (!haveBindingGroups)
						displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

					// Create display string.
					var displayString = action.GetBindingDisplayString(i, displayOptions);

					// If binding is part of a composite, include the part name.
					if (binding.isPartOfComposite)
						displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

					// Some composites use '/' as a separator. When used in popup, this will lead to to submenus. Prevent
					// by instead using a backlash.
					displayString = displayString.Replace('/', '\\');

					// If the binding is part of control schemes, mention them.
					if (haveBindingGroups) {
						var asset = action.actionMap?.asset;
						if (asset != null) {
							var controlSchemes = string.Join(", ",
								binding.groups.Split(InputBinding.Separator)
									.Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

							displayString = $"{displayString} ({controlSchemes})";
						}
					}

					bindingOptions[i] = new GUIContent(displayString);
					bindingOptionValues[i] = bindingId;

					if (currentBindingId == bindingId)
						selectedBindingOption = i;
				}
			}



			position.height /= 2f;
			EditorGUI.PropertyField(position, actionProperty);

			position.y += position.height;
			var newSelectedBinding = EditorGUI.Popup(position, new GUIContent("Binding"), selectedBindingOption, bindingOptions);
			if (newSelectedBinding != selectedBindingOption) {
				var bindingId = bindingOptionValues[newSelectedBinding];
				bindingIdProperty.stringValue = bindingId;
				selectedBindingOption = newSelectedBinding;
			}

			EditorGUI.EndProperty();
		}
	}
#endif

}
#endif