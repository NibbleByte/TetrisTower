#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Activates objects when specified control scheme is active, deactivates the rest.
	/// Useful to change UI layout when switching from keyboard to gamepad and vice versa.
	/// </summary>
	public class InputControlSchemeActivatorUI : MonoBehaviour
	{
		[Serializable]
		public struct ControlSchemeActiveObjects
		{
			[Tooltip("Control scheme used in your .inputactions asset.")]
			[InputControlSchemePicker]
			public string ControlScheme;

			[NonReorderable]
			public GameObject[] Objects;
		}

		public ControlSchemeActiveObjects[] ControlSchemeObjects;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		private string m_LastControlScheme;

		private void RefreshObjects(IInputContext context)
		{
			InputControlScheme scheme = context.GetLastUsedInputControlScheme();

			if (scheme.bindingGroup == m_LastControlScheme)
				return;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				SetObjectsActive(m_LastControlScheme, false);
			}

			m_LastControlScheme = scheme.bindingGroup;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				SetObjectsActive(m_LastControlScheme, true);
			}
		}

		private void SetObjectsActive(string controlScheme, bool active)
		{
			foreach(ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				if (bind.ControlScheme.Equals(controlScheme, StringComparison.OrdinalIgnoreCase)) {
					foreach(GameObject obj in bind.Objects) {
						if (obj) {
							obj.SetActive(active);
						}
					}
					return;
				}
			}
		}

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			foreach (ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				foreach(GameObject obj in bind.Objects) {
					if (obj) {
						obj.SetActive(false);
					}
				}
			}
		}

		void OnEnable()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedDeviceChanged += OnLastUsedDeviceChanged;
			m_LastControlScheme = null;
			RefreshObjects(m_PlayerContext.InputContext);
		}

		void OnDisable()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;
		}

		private void OnLastUsedDeviceChanged()
		{

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			RefreshObjects(m_PlayerContext.InputContext);
		}

		void OnValidate()
		{
			if (ControlSchemeObjects.SelectMany(bind => bind.Objects).Any(obj => obj && transform.IsChildOf(obj.transform))) {
				Debug.LogError($"{nameof(InputControlSchemeActivatorUI)} deactivates game objects that are parents of it. This is not allowed.", this);
			}
		}
	}

}
#endif