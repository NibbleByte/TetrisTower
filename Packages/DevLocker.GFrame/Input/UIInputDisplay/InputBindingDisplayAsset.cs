#if USE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace DevLocker.GFrame.Input.UIInputDisplay
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
			[InputControl]
			public string BindingPath;

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

		[Tooltip("Is UI navigation with selected element allowed when this type of device is used?")]
		[SerializeField]
		private bool m_SupportsUINavigationSelection = true;
		public bool SupportsUINavigationSelection => m_SupportsUINavigationSelection;

		[Space]
		[Tooltip("(Optional) Format selected binding display text if it doesn't use sprites.\n\"{binding}\" will be replaced with the binding display text.")]
		public string FormatBindingTexts = "";
		[Tooltip("(Optional) Format selected binding display text if it contains sprites.\n\"{binding}\" will be replaced with the binding display text.")]
		public string FormatBindingSprites = "";

		[Space]
		[InputControlSchemePicker]
		[Tooltip("The control scheme that matches the devices listed below.")]
		public string MatchingControlScheme;
		public string[] MatchingDeviceLayouts;

		public BindingDisplayAssetsData[] BindingDisplays;

		[NonSerialized]
		private InputBinding m_ControlSchemeMatchBinding = new InputBinding();
		private KeyValuePair<InputBinding, BindingDisplayAssetsData>[] m_BindingDisplaysAssetsCache;


		public string FormatBindingDisplayText(string displayText)
		{
			if (string.IsNullOrWhiteSpace(displayText))
				return displayText;

			return displayText.Contains("<sprite")
				? (string.IsNullOrWhiteSpace(FormatBindingSprites) ? displayText : FormatBindingSprites.Replace("{binding}", displayText, StringComparison.OrdinalIgnoreCase))
				: (string.IsNullOrWhiteSpace(FormatBindingTexts) ? displayText :FormatBindingTexts.Replace("{binding}", displayText, StringComparison.OrdinalIgnoreCase))
				;
		}

		public bool MatchesBinding(InputBinding binding)
		{
			return string.IsNullOrWhiteSpace(binding.groups) ? false : binding.groups.Contains(MatchingControlScheme);
		}

		public bool MatchesDevice(string deviceLayout)
		{
			return MatchingDeviceLayouts.Contains(deviceLayout, StringComparer.OrdinalIgnoreCase);
		}

		public IEnumerable<InputBindingDisplayData> GetBindingDisplaysFor(InputAction action)
		{
			if (string.IsNullOrWhiteSpace(MatchingControlScheme)) {
				Debug.LogError($"[Input] Matching control scheme is missing for {name}!", this);
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
						Debug.LogError($"[Input] Action {action.name} has composite binding {binding.name} with no parts.", this);
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
				//bool matches = pair.Key.Matches(binding);

				// TODO: InputBinding.Matches() uses the .path instead of .effectivePath. There are some REVIEW comments to fix this, but no one knows when.
				bool matches = pair.Key.path.Equals(binding.effectivePath, StringComparison.OrdinalIgnoreCase);

				if (matches || (binding.isComposite && pair.Key.path.Equals(binding.name, StringComparison.OrdinalIgnoreCase))) {
					var bindingDisplay = new InputBindingDisplayData {
						Binding = binding,
						BindingIndex = bindingIndex,
						ControlScheme = MatchingControlScheme,
					};

					if (pair.Value.UseDefaultTexts) {
						try {
							bindingDisplay.Text = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
							bindingDisplay.ShortText = action.GetBindingDisplayString(bindingIndex);
						} catch(NotImplementedException) {
							// HACK: current version of the InputSystem 1.3.0 doesn't support texts for special bindings like "*/{Submit}".
							// This is what they say in the MatchControlsRecursive():
							////TODO: support scavenging a subhierarchy for usages
							//throw new NotImplementedException("Matching usages inside subcontrols instead of at device root");

							bindingDisplay.Text = string.Empty;
							bindingDisplay.ShortText = string.Empty;
						}

					} else {
						bindingDisplay.Text = pair.Value.DisplayText;
						bindingDisplay.ShortText = pair.Value.DisplayShortText;
					}

					return bindingDisplay;
				}
			}

			if (FallbackToDefaultDisplayTexts) {
				string text;
				string shortText;

				try {
					text = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
					shortText = action.GetBindingDisplayString(bindingIndex);
				}
				catch (NotImplementedException) {
					// HACK: current version of the InputSystem 1.3.0 doesn't support texts for special bindings like "*/{Submit}".
					// This is what they say in the MatchControlsRecursive():
					////TODO: support scavenging a subhierarchy for usages
					//throw new NotImplementedException("Matching usages inside subcontrols instead of at device root");

					text = string.Empty;
					shortText = string.Empty;
				}

				return new InputBindingDisplayData {
					Binding = binding,
					BindingIndex = bindingIndex,
					ControlScheme = MatchingControlScheme,
					Text = text,
					ShortText = shortText,
				};
			}

			return new InputBindingDisplayData() { BindingIndex = -1 };
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(InputBindingDisplayAsset.BindingDisplayAssetsData))]
	internal class InputBindingDisplayAssetBindingDisplayAssetsDataPropertyDrawer : UnityEditor.PropertyDrawer
	{
		private static float s_LineHeight => UnityEditor.EditorGUIUtility.singleLineHeight + UnityEditor.EditorGUIUtility.standardVerticalSpacing;

		public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
				return UnityEditor.EditorGUIUtility.singleLineHeight;

			var useDefaultTextsProperty = property.FindPropertyRelative(nameof(InputBindingDisplayAsset.BindingDisplayAssetsData.UseDefaultTexts));

			float height = s_LineHeight * 2 + UnityEditor.EditorGUIUtility.singleLineHeight;
			if (!useDefaultTextsProperty.boolValue) {
				height += s_LineHeight * 2;
			}

			return height;
		}

		public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
		{
			label = UnityEditor.EditorGUI.BeginProperty(position, label, property);

			Rect lineRect = position;
			lineRect.height = s_LineHeight;

			property.isExpanded = UnityEditor.EditorGUI.Foldout(lineRect, property.isExpanded, label, true);
			lineRect.y += s_LineHeight;

			if (property.isExpanded) {
				UnityEditor.EditorGUI.indentLevel++;

				UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(InputBindingDisplayAsset.BindingDisplayAssetsData.BindingPath)));

				lineRect.y += s_LineHeight;

				var useDefaultTextsProperty = property.FindPropertyRelative(nameof(InputBindingDisplayAsset.BindingDisplayAssetsData.UseDefaultTexts));
				UnityEditor.EditorGUI.PropertyField(lineRect, useDefaultTextsProperty);

				lineRect.y += s_LineHeight;

				if (!useDefaultTextsProperty.boolValue) {
					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(InputBindingDisplayAsset.BindingDisplayAssetsData.DisplayText)));

					lineRect.y += s_LineHeight;

					UnityEditor.EditorGUI.PropertyField(lineRect, property.FindPropertyRelative(nameof(InputBindingDisplayAsset.BindingDisplayAssetsData.DisplayShortText)));

					lineRect.y += s_LineHeight;
				}

				UnityEditor.EditorGUI.indentLevel--;
			}

			UnityEditor.EditorGUI.EndProperty();
		}
	}
#endif
}
#endif