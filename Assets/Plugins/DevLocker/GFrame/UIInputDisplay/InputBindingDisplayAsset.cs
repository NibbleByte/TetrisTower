#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.UIInputDisplay
{
	/// <summary>
	/// ScriptableObject containing the required assets to display hotkeys in the UI for specific device.
	/// </summary>
	[CreateAssetMenu(fileName = "InputBindingDisplayAsset", menuName = "GFrame/Input Bindings Display Asset", order = 1010)]
	public class InputBindingDisplayAsset : ScriptableObject, IInputBindingDisplayDataProvider
	{
		[Serializable]
		public struct BindingDisplayAssetsData
		{
			// TODO: UPDATE TO InputControl WHEN FIXED
			[InputControlFIXED]
			public string BindingPath;

			public Sprite Icon;

			[Tooltip("Use the InputBinding displayName provided by Unity instead.\nIf true, next text fields will be ignored.")]
			public bool UseDefaultTexts;

			public string DisplayText;
			public string DisplayShortText;
		}

		public string DeviceName;
		public Sprite DeviceIcon;
		public Color DeviceColor;

		[Tooltip("If one of the action's bindings doesn't have a defined display data in the list below, use the default display name provided by Unity.")]
		public bool FallbackToDefaultDisplayTexts = true;

		[Space()]
		public string[] MatchingDeviceLayouts;

		public BindingDisplayAssetsData[] BindingDisplays;

		[NonSerialized]
		private InputBinding m_ControlSchemeMatchBinding = new InputBinding();
		private KeyValuePair<InputBinding, BindingDisplayAssetsData>[] m_BindingDisplaysAssetsCache;


		public bool MatchesDevice(InputDevice device)
		{
			return MatchingDeviceLayouts.Contains(device.layout, StringComparer.OrdinalIgnoreCase);
		}

		public IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(InputControlScheme controlScheme, InputAction action)
		{
			if (m_BindingDisplaysAssetsCache == null) {
				m_BindingDisplaysAssetsCache = new KeyValuePair<InputBinding, BindingDisplayAssetsData>[BindingDisplays.Length];

				for(int i = 0; i < BindingDisplays.Length; ++i) {
					BindingDisplayAssetsData bindingDisplay = BindingDisplays[i];
					m_BindingDisplaysAssetsCache[i] = new KeyValuePair<InputBinding, BindingDisplayAssetsData>(new InputBinding(bindingDisplay.BindingPath), bindingDisplay);
				}
			}

			// Should never happen?
			if (string.IsNullOrEmpty(controlScheme.bindingGroup))
				yield break;

			m_ControlSchemeMatchBinding.groups = controlScheme.bindingGroup;

			foreach(InputBinding binding in action.bindings) {

				// InputBinding.Matches() compares semantically the binding. In case you have ";Keyboard&Mouse" etc...
				if (!m_ControlSchemeMatchBinding.Matches(binding))
					continue;

				bool found = false;
				foreach(var pair in m_BindingDisplaysAssetsCache) {

					// InputBinding.Matches() compares semantically the binding. In case you have "<Keyboard>/space;<Keyboard>/enter" etc...
					if (pair.Key.Matches(binding)) {
						var bindingDisplay = new InputBindingDisplayData {
							Binding = binding,
							Icon = pair.Value.Icon,
						};

						if (pair.Value.UseDefaultTexts) {
							bindingDisplay.Text = binding.ToDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
							bindingDisplay.ShortText = binding.ToDisplayString();
						} else {
							bindingDisplay.Text = pair.Value.DisplayText;
							bindingDisplay.ShortText = pair.Value.DisplayShortText;
						}

						yield return bindingDisplay;
						found = true;
						break;
					}
				}

				if (!found && FallbackToDefaultDisplayTexts) {

					var bindingDisplay = new InputBindingDisplayData {
						Binding = binding,
						Icon = null,
						Text = binding.ToDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames),
						ShortText = binding.ToDisplayString(),
					};

					yield return bindingDisplay;
				}
			}
		}
	}

	/// <summary>
	/// TODO: REMOVE WHEN FIXED
	/// InputControlPathDrawer drawer doesn't work properly when used in lists - made a temporary fix until this gets resolved.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class InputControlFIXEDAttribute : PropertyAttribute
	{

	}


#if UNITY_EDITOR
	/// <summary>
	/// TODO: REMOVE WHEN FIXED
	/// InputControlPathDrawer drawer doesn't work properly when used in lists - made a temporary fix until this gets resolved.
	/// </summary>
	[UnityEditor.CustomPropertyDrawer(typeof(InputControlFIXEDAttribute))]
	internal sealed class InputControlPathDrawer : UnityEditor.PropertyDrawer
	{
		private UnityEngine.InputSystem.Editor.InputControlPickerState m_PickerState;

		public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
		{
			if (m_PickerState == null)
				m_PickerState = new UnityEngine.InputSystem.Editor.InputControlPickerState();

			var editor = new UnityEngine.InputSystem.Editor.InputControlPathEditor(property, m_PickerState,
				() => property.serializedObject.ApplyModifiedProperties(),
				label: label);
			editor.SetExpectedControlLayoutFromAttribute();

			UnityEditor.EditorGUI.BeginProperty(position, label, property);
			editor.OnGUI(position);
			UnityEditor.EditorGUI.EndProperty();

			editor.Dispose();
		}
	}
#endif

}
#endif