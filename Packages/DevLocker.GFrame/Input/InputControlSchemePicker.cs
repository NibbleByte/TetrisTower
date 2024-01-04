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
	/// Adds options to select InputControlScheme string from the assets in the project.
	/// Use this to avoid typos.
	/// NOTE: If control schemes change, strings won't.
	/// </summary>
	public class InputControlSchemePickerAttribute : PropertyAttribute
	{
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputControlSchemePickerAttribute))]
	internal class InputControlSchemePickerPropertyDrawer : PropertyDrawer
	{
		private GUIContent m_PickerButtonContent;
		private GUIStyle m_PickerButtonStyle;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			//EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			const float PickerButtonWidth = 32f;
			const float PickerButtonMargin = 4f;

			position.width -= PickerButtonWidth + PickerButtonMargin;
			EditorGUI.PropertyField(position, property, label);

			position.x += position.width + PickerButtonMargin;
			position.width = PickerButtonWidth;

			if (m_PickerButtonContent == null) {
				m_PickerButtonContent = EditorGUIUtility.IconContent("Search Icon");
				m_PickerButtonContent.tooltip = "Pick input control scheme from the project.";
				m_PickerButtonStyle = new GUIStyle(GUI.skin.button);
				m_PickerButtonStyle.padding = new RectOffset();
			}

			if (GUI.Button(position, m_PickerButtonContent, m_PickerButtonStyle)) {
				var assets = AssetDatabase.FindAssets($"t:{typeof(InputActionAsset).Name}")
					.Select(AssetDatabase.GUIDToAssetPath)
					.Where(path => !path.StartsWith("Packages/com.unity.inputsystem", StringComparison.OrdinalIgnoreCase))
					.Select(AssetDatabase.LoadAssetAtPath<InputActionAsset>)
					.ToList()
					;

				var menu = new GenericMenu();
				foreach(InputActionAsset asset in assets) {
					foreach(InputControlScheme scheme in asset.controlSchemes) {

						string bindingGroup = scheme.bindingGroup; // Careful! Closure!
						menu.AddItem(new GUIContent($"{asset.name} - {bindingGroup}"), false, () => {
							property.serializedObject.Update();
							property.stringValue = bindingGroup;
							property.serializedObject.ApplyModifiedProperties();
						});
					}

					if (asset != assets.Last()) {
						menu.AddSeparator("");
					}
				}

				menu.ShowAsContext();
			}

			EditorGUI.EndProperty();
		}
	}
#endif

}
#endif