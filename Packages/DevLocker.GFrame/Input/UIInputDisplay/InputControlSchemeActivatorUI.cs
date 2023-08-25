#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
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
			public List<GameObject> Objects;
		}

		public List<ControlSchemeActiveObjects> ControlSchemeObjects;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		private string m_LastControlScheme;

		private void RefreshObjects(IInputContext context)
		{
			InputControlScheme scheme = context.GetLastUsedInputControlScheme();

			if (scheme.bindingGroup == m_LastControlScheme)
				return;

			// First deactivate, then activate, so enable events happen in correct order.
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
			foreach (ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				if (bind.ControlScheme.Equals(controlScheme, StringComparison.OrdinalIgnoreCase)) {
					foreach(GameObject obj in bind.Objects) {
						obj.SetActive(active);
					}
					return;
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

		void OnDestroy()
		{
			// Remove references for easier memory profiling and debugging. NOTE: if object was never awaken, this won't get executed.
			m_PlayerContext = null;

			ControlSchemeObjects.Clear();
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

			if (string.IsNullOrEmpty(m_LastControlScheme)) {
				foreach (ControlSchemeActiveObjects bind in ControlSchemeObjects) {
					SetObjectsActive(bind.ControlScheme, false);
				}
			}

			m_LastControlScheme = null;
			RefreshObjects(m_PlayerContext.InputContext);
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			if (m_PlayerContext?.InputContext != null) {
				m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged -= OnLastUsedInputControlSchemeChanged;
			}

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				foreach (ControlSchemeActiveObjects bind in ControlSchemeObjects) {
					SetObjectsActive(bind.ControlScheme, false);
				}
			}
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