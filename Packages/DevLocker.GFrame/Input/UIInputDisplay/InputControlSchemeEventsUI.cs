#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace DevLocker.GFrame.Input.UIInputDisplay
{
	/// <summary>
	/// Executes a UnityEvent when specified control schemes become active/inactive (last used).
	/// </summary>
	public class InputControlSchemeEventsUI : MonoBehaviour
	{
		[Serializable]
		public struct ControlSchemeActionSettings
		{
			[Tooltip("Control scheme used in your .inputactions asset.")]
			[InputControlSchemePicker]
			public string ControlScheme;

			[Tooltip("Should it trigger the inactive event on the first OnEnable (i.e. initialize)?")]
			public bool TriggerInactiveOnInit;

			public UnityEvent OnSchemeActive;
			public UnityEvent OnSchemeInactive;
		}

		public List<ControlSchemeActionSettings> ControlSchemeActions;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		private string m_LastControlScheme;

		private void ExecuteActions(IInputContext context)
		{
			InputControlScheme scheme = context.GetLastUsedInputControlScheme();

			if (scheme.bindingGroup == m_LastControlScheme)
				return;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				ExecuteAction(m_LastControlScheme, false);
			}

			m_LastControlScheme = scheme.bindingGroup;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				ExecuteAction(m_LastControlScheme, true);
			}
		}

		private void ExecuteAction(string controlScheme, bool active)
		{
			foreach(ControlSchemeActionSettings bind in ControlSchemeActions) {
				if (bind.ControlScheme.Equals(controlScheme, StringComparison.OrdinalIgnoreCase)) {
					UnityEvent action = active ? bind.OnSchemeActive : bind.OnSchemeInactive;
					action.Invoke();
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

		void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeEventsUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged += OnLastUsedInputControlSchemeChanged;

			if (string.IsNullOrEmpty(m_LastControlScheme)) {
				foreach (ControlSchemeActionSettings bind in ControlSchemeActions) {
					if (bind.TriggerInactiveOnInit) {
						ExecuteAction(bind.ControlScheme, false);
					}
				}
			}

			m_LastControlScheme = null;
			ExecuteActions(m_PlayerContext.InputContext);
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			// Changing levels or stopping the game - don't trigger events as this may cause headaches.
			if (!gameObject.scene.isLoaded)
				return;

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeEventsUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			m_PlayerContext.InputContext.LastUsedInputControlSchemeChanged -= OnLastUsedInputControlSchemeChanged;


			if (!string.IsNullOrEmpty(m_LastControlScheme)) {

				foreach (ControlSchemeActionSettings bind in ControlSchemeActions) {
					if (m_LastControlScheme == bind.ControlScheme) {
						ExecuteAction(bind.ControlScheme, false);
						break;
					}
				}
			}
		}

		private void OnLastUsedInputControlSchemeChanged()
		{
			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"[Input] {nameof(InputControlSchemeEventsUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			ExecuteActions(m_PlayerContext.InputContext);
		}
	}

}
#endif