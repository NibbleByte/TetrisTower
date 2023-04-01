#if USE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Activate objects if their hotkey is enabled.
	/// Useful for making hotkeys bar / legend at the bottom of your screen.
	/// </summary>
	public class HotkeysDisplayList : MonoBehaviour
	{
		[Tooltip("Will recalculate layout after hotkeys change is detected. Recommended if you use layout groups + content size fitters.")]
		public bool RecalculateLayoutOnChange = true;

		[Tooltip("Will be active only if the InputAction itself is enabled.")]
		public HotkeyDisplayUI[] HotkeyObjects;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		private void OnDisable()
		{
			foreach(var hotkey in HotkeyObjects) {
				if (hotkey) {
					hotkey.gameObject.SetActive(false);
				}
			}
		}

		void LateUpdate()
		{
			bool hasChanges = false;

			foreach (var hotkey in HotkeyObjects) {
				if (hotkey == null)
					continue;

				bool actionEnabled = m_PlayerContext.IsActive && (m_PlayerContext.InputContext?.FindActionFor(hotkey.InputAction.name)?.enabled ?? false);
				if (actionEnabled != hotkey.gameObject.activeSelf) {
					hotkey.gameObject.SetActive(actionEnabled);
					hasChanges = true;
				}
			}

			// Make sure layouts refresh correctly.
			if (RecalculateLayoutOnChange && hasChanges) {
				Utils.UIUtils.ForceRecalclulateLayouts((RectTransform) (transform));
			}
		}
	}
}

#endif