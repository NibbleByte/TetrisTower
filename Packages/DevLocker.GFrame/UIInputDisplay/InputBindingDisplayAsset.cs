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
		public Sprite DeviceIconSmall;
		public Color DeviceColor;

		[Tooltip("If one of the action's bindings doesn't have a defined display data in the list below, use the default display name provided by Unity.")]
		public bool FallbackToDefaultDisplayTexts = true;

		[Space()]
		[InputControlSchemePicker]
		[Tooltip("The control scheme that matches the devices listed below.")]
		public string MatchingControlScheme;
		public string[] MatchingDeviceLayouts;

		public BindingDisplayAssetsData[] BindingDisplays;

		[NonSerialized]
		private InputBinding m_ControlSchemeMatchBinding = new InputBinding();
		private KeyValuePair<InputBinding, BindingDisplayAssetsData>[] m_BindingDisplaysAssetsCache;


		public bool MatchesDevice(string deviceLayout)
		{
			return MatchingDeviceLayouts.Contains(deviceLayout, StringComparer.OrdinalIgnoreCase);
		}

		public IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(InputAction action)
		{
			if (string.IsNullOrWhiteSpace(MatchingControlScheme)) {
				Debug.LogError($"Matching control scheme is missing for {name}!", this);
				yield break;
			}

			m_ControlSchemeMatchBinding.groups = MatchingControlScheme;

			var bindings = action.bindings;
			for(int i = 0; i < bindings.Count; ++i) {
				int bindingIndex = i;
				InputBinding binding = bindings[bindingIndex];
				var compositeBindingParts = new List<InputBindingDisplayData>();

				if (binding.isComposite) {
					++i;

					// Assume the other parts are of the same scheme.
					if (!m_ControlSchemeMatchBinding.Matches(bindings[i]))
						continue;

					if (i >= bindings.Count || !bindings[i].isPartOfComposite) {
						Debug.LogError($"Action {action.name} has composite binding {binding.name} with no parts.", this);
						continue;
					}

					for(; i < bindings.Count && bindings[i].isPartOfComposite; ++i) {
						InputBindingDisplayData bindingPartDisplay = PrepareDisplayDataFor(action, bindings[i], i);
						compositeBindingParts.Add(bindingPartDisplay);
					}

					--i;	// Compensate for the initial for-loop iteration.

				} else if (!m_ControlSchemeMatchBinding.Matches(binding)) {
					// InputBinding.Matches() compares semantically the binding. In case you have ";Keyboard&Mouse" etc...
					continue;
				}

				InputBindingDisplayData bindingDisplay = PrepareDisplayDataFor(action, binding, bindingIndex);
				bindingDisplay.CompositeBindingParts = compositeBindingParts;

				if (binding.isComposite) {
					// Composite bindings should always be valid.
					// Their parts are the important ones if no display data found for this one.
					bindingDisplay.Binding = binding;
				}

				if (bindingDisplay.IsValid)
					yield return bindingDisplay;
			}
		}

		private InputBindingDisplayData PrepareDisplayDataFor(InputAction action, InputBinding binding, int bindingIndex)
		{
			if (m_BindingDisplaysAssetsCache == null) {
				m_BindingDisplaysAssetsCache = new KeyValuePair<InputBinding, BindingDisplayAssetsData>[BindingDisplays.Length];

				for (int i = 0; i < BindingDisplays.Length; ++i) {
					BindingDisplayAssetsData bindingDisplay = BindingDisplays[i];
					m_BindingDisplaysAssetsCache[i] = new KeyValuePair<InputBinding, BindingDisplayAssetsData>(new InputBinding(bindingDisplay.BindingPath), bindingDisplay);
				}
			}

			foreach (var pair in m_BindingDisplaysAssetsCache) {

				// InputBinding.Matches() compares semantically the binding. In case you have "<Keyboard>/space;<Keyboard>/enter" etc...
				// In case of composite binding, path is an invalid parameter to match on. Use the name instead.
				if (pair.Key.Matches(binding) || (binding.isComposite && pair.Key.path.Equals(binding.name, StringComparison.OrdinalIgnoreCase))) {
					var bindingDisplay = new InputBindingDisplayData {
						Binding = binding,
						Icon = pair.Value.Icon,
					};

					if (pair.Value.UseDefaultTexts) {
						bindingDisplay.Text = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
						bindingDisplay.ShortText = action.GetBindingDisplayString(bindingIndex);
					} else {
						bindingDisplay.Text = pair.Value.DisplayText;
						bindingDisplay.ShortText = pair.Value.DisplayShortText;
					}

					return bindingDisplay;
				}
			}

			if (FallbackToDefaultDisplayTexts) {

				return new InputBindingDisplayData {
					Binding = binding,
					Icon = null,
					Text = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames),
					ShortText = action.GetBindingDisplayString(bindingIndex),
				};
			}

			return new InputBindingDisplayData();
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