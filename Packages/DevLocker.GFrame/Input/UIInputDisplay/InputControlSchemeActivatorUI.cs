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

		protected bool m_HasInitialized = false;

		private void RefreshObjects(IInputContext context)
		{
			string scheme = context.GetLastUsedInputControlScheme().bindingGroup ?? string.Empty;

			foreach (ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				bool active = bind.ControlScheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);

				foreach (GameObject obj in bind.Objects) {
					if (obj) {
						obj.SetActive(active);
					}
				}
			}
		}

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

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

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged += OnLastUsedInputControlSchemeChanged;

			RefreshObjects(m_PlayerContext.InputContext);
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged -= OnLastUsedInputControlSchemeChanged;
		}

		private void OnLastUsedInputControlSchemeChanged()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			RefreshObjects(m_PlayerContext.InputContext);
		}

		void OnValidate()
		{
			if (ControlSchemeObjects == null)
				return;

			if (ControlSchemeObjects.SelectMany(bind => bind.Objects).Any(obj => obj == null)) {
				Debug.LogError($"[Input] {nameof(InputControlSchemeActivatorUI)} has missing target objects.", this);
			}
			if (ControlSchemeObjects.SelectMany(bind => bind.Objects).Any(obj => obj && transform.IsChildOf(obj.transform))) {
				Debug.LogError($"[Input] {nameof(InputControlSchemeActivatorUI)} deactivates game objects that are parents of it. This is not allowed.", this);
			}
		}
	}

}
#endif