#if USE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Attach this component next to <see cref="TextMeshProUGUI"/> and it will replace and update any displayed InputActions in the text.
	/// Input actions should be surrounded by curly braces: {Jump}
	/// Text changes by other code are automatically detected and refreshed.
	/// </summary>
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class TextInlineInputDisplayUI : MonoBehaviour
	{
		private class OwnedAction
		{
			public string OriginalText;
			public string DisplayText;
		}

		[Tooltip("Disable the text mesh pro component if input action for the current device is unavailable.\nIf layout element is present on this object, it will set it to ignore the layout as well.")]
		public bool HideTextIfBindingUnavailable = true;

		[Space]
		[Tooltip("(Optional) Format selected binding display text if it doesn't use sprites.\n\"{binding}\" will be replaced with the binding display text.")]
		public string FormatBindingTexts = "";
		[Tooltip("(Optional) Format selected binding display text if it contains sprites.\n\"{binding}\" will be replaced with the binding display text.")]
		public string FormatBindingSprites = "";

		private static Regex s_ActionPattern = new Regex(@"{[\w]+}");

		private TextMeshProUGUI m_Text;
		private LayoutElement m_LayoutElement;
		private bool m_ChangingText = false;
		private Dictionary<InputAction, OwnedAction> m_ManagedActions = new Dictionary<InputAction, OwnedAction>();
		private List<InputAction> m_RemoveDictionaryCache = new List<InputAction>();

		private IInputBindingDisplayDataProvider m_LastDisplayDataProvider;


		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			m_Text = GetComponent<TextMeshProUGUI>();
			m_LayoutElement = GetComponent<LayoutElement>();

			m_PlayerContext.AddSetupCallback((delayedSetup) => {
				m_HasInitialized = true;

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			if (m_Text) {
				// This will be called right after the change happens, but before layout rebuild happens, so it shouldn't be slow.
				m_Text.RegisterDirtyLayoutCallback(TextLayoutChanged);
			}

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(TextInlineInputDisplayUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedDeviceChanged += OnLastUsedDeviceChanged;

			OnLastUsedDeviceChanged();
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			if (m_Text) {
				m_Text.UnregisterDirtyLayoutCallback(TextLayoutChanged);
			}

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(TextInlineInputDisplayUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;
		}

		public void RefreshTextInputSprites()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(TextInlineInputDisplayUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			var currentProvider = m_PlayerContext.InputContext.GetCurrentDisplayDataProvider();
			string text = m_Text.text;
			bool hasChanges = false;
			bool shouldHideText = false;

			if (m_LastDisplayDataProvider != null && m_LastDisplayDataProvider != currentProvider) {

				foreach(var pair in m_ManagedActions) {
					InputAction action = pair.Key;

					// Text could have changed in the mean time so our doing was overwritten.
					string lastDisplayText = pair.Value.DisplayText;
					if (!text.Contains(lastDisplayText) || string.IsNullOrEmpty(lastDisplayText)) {
						m_RemoveDictionaryCache.Add(action);
						continue;
					}

					string currentDisplayText = GetDisplayTextFor(action, currentProvider);

					// If new provider doesn't have visuals for this binding, restore the initial text.
					if (string.IsNullOrEmpty(currentDisplayText)) {
						currentDisplayText = pair.Value.OriginalText;
						shouldHideText = true;
						m_RemoveDictionaryCache.Add(action);
					}

					pair.Value.DisplayText = currentDisplayText;
					text = text.Replace(lastDisplayText, currentDisplayText);
					hasChanges = true;
				}

				foreach(InputAction action in m_RemoveDictionaryCache) {
					m_ManagedActions.Remove(action);
				}
				m_RemoveDictionaryCache.Clear();
			}

			m_LastDisplayDataProvider = currentProvider;

			MatchCollection matches = s_ActionPattern.Matches(text);
			if (matches.Count == 0) {
				m_Text.enabled = !HideTextIfBindingUnavailable || !shouldHideText;
				if (m_LayoutElement) {
					m_LayoutElement.ignoreLayout = !m_Text.enabled;
				}

				if (hasChanges) {
					m_ChangingText = true;
					m_Text.text = text;
					m_ChangingText = false;
				}

				return;
			}


			StringBuilder replaced = new StringBuilder();

			int prevIndex = 0;

			foreach (Match match in matches) {
				replaced.Append(text.Substring(prevIndex, match.Index - prevIndex));
				prevIndex = match.Index + match.Value.Length;

				// Remove the curly braces {}
				string actionName = match.Value.Substring(1, match.Value.Length - 2);
				InputAction action = m_PlayerContext.InputContext.FindActionFor(actionName);

				string displayText = GetDisplayTextFor(action, currentProvider);
				if (!string.IsNullOrEmpty(displayText)) {

					if (!m_ManagedActions.ContainsKey(action)) {
						m_ManagedActions.Add(action, new OwnedAction() {
							OriginalText = match.Value,
							DisplayText = displayText
						});
					}
					replaced.Append(displayText);
				} else {
					shouldHideText = true;
					replaced.Append(match.Value);
				}
			}

			replaced.Append(text.Substring(prevIndex, text.Length - prevIndex));

			m_Text.enabled = !HideTextIfBindingUnavailable || !shouldHideText;
			if (m_LayoutElement) {
				m_LayoutElement.ignoreLayout = !m_Text.enabled;
			}

			m_ChangingText = true;
			m_Text.text = replaced.ToString();
			m_ChangingText = false;
		}

		private string GetDisplayTextFor(InputAction action, IInputBindingDisplayDataProvider displayDataProvider)
		{
			// Consider only the first binding match.
			InputBindingDisplayData displayData = displayDataProvider.GetBindingDisplaysFor(action).FirstOrDefault();
			if (!displayData.IsValid)
				return string.Empty;

			if (displayData.Text.Contains("<sprite")) {
				string locallyFormatted = string.IsNullOrWhiteSpace(FormatBindingSprites) ? displayData.Text : FormatBindingSprites.Replace("{binding}", displayData.Text, StringComparison.OrdinalIgnoreCase);
				return displayDataProvider.FormatBindingDisplayText(locallyFormatted);
			} else {
				// Add <b> tag to store the input action in an attribute so we can recognize it later and update it if needed.
				string displayText = $"<b inputAction=\"{action.name}\">{displayData.Text}</b>";
				string locallyFormatted = string.IsNullOrWhiteSpace(FormatBindingTexts) ? displayText : FormatBindingTexts.Replace("{binding}", displayText, StringComparison.OrdinalIgnoreCase);

				return displayDataProvider.FormatBindingDisplayText(locallyFormatted);
			}
		}

		private void TextLayoutChanged()
		{
			// Called by change we are making at the moment.
			if (m_ChangingText)
				return;

			RefreshTextInputSprites();
		}

		private void OnLastUsedDeviceChanged()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(TextInlineInputDisplayUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			var currentProvider = m_PlayerContext.InputContext.GetCurrentDisplayDataProvider();

			if (m_LastDisplayDataProvider == null || m_LastDisplayDataProvider != currentProvider) {
				// NOTE: This will not update on changing keyboard layout/language. Someday...
				RefreshTextInputSprites();
			}
		}
	}

}
#endif