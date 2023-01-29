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
				//bool matches = pair.Key.Matches(binding);

				// TODO: InputBinding.Matches() uses the .path instead of .effectivePath. There are some REVIEW comments to fix this, but no one knows when.
				bool matches = pair.Key.path.Equals(binding.effectivePath, StringComparison.OrdinalIgnoreCase);

				if (matches || (binding.isComposite && pair.Key.path.Equals(binding.name, StringComparison.OrdinalIgnoreCase))) {
					var bindingDisplay = new InputBindingDisplayData {
						Binding = binding,
						BindingIndex = bindingIndex,
						ControlScheme = MatchingControlScheme,
						Icon = pair.Value.Icon,
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
					Icon = null,
					Text = text,
					ShortText = shortText,
				};
			}

			return new InputBindingDisplayData() { BindingIndex = -1 };
		}
	}

}
#endif