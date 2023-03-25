#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Base class for hotkey scope elements (that use Unity's Input System).
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public abstract class HotkeyBaseScopeElement : MonoBehaviour, IScopeElement, IHotkeyWithInputAction
	{
		[Tooltip("Skip the hotkey based on the selected condition.")]
		[Utils.EnumMask]
		public SkipHotkeyOption SkipHotkey;

		[SerializeField]
		protected InputActionReference m_InputAction;

		protected bool m_ActionStarted { get; private set; } = false;
		protected bool m_ActionPerformed { get; private set; } = false;

		protected List<InputAction> m_SubscribedActions = new List<InputAction>();

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		protected virtual void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			m_PlayerContext.AddSetupCallback((delayedSetup) => {
				m_HasInitialized = true;

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		protected virtual void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			if (m_PlayerContext.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyButtonScopeElement)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			foreach(InputAction action in GetUsedActions(m_PlayerContext.InputContext)) {
				m_SubscribedActions.Add(action);
				action.started += OnInputStarted;
				action.performed += OnInputPerformed;
				action.canceled += OnInputCancel;
			}
		}

		protected virtual void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			if (m_PlayerContext.InputContext == null)
				return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			foreach (InputAction action in m_SubscribedActions) {
				action.started -= OnInputStarted;
				action.performed -= OnInputPerformed;
				action.canceled -= OnInputCancel;
			}

			m_SubscribedActions.Clear();
		}

		private void OnInputStarted(InputAction.CallbackContext obj)
		{
			var selected = m_PlayerContext.SelectedGameObject;

			if ((SkipHotkey & SkipHotkeyOption.NonTextSelectableFocused) != 0
				&& selected
				&& !selected.GetComponent<InputField>()
#if USE_TEXT_MESH_PRO
				&& !selected.GetComponent<TMPro.TMP_InputField>()
#endif
				)
				return;

			if ((SkipHotkey & SkipHotkeyOption.InputFieldTextFocused) != 0 && selected) {
				var inputField = selected.GetComponent<InputField>();
				if (inputField && inputField.isFocused)
					return;

#if USE_TEXT_MESH_PRO
				var inputFieldTMP = selected.GetComponent<TMPro.TMP_InputField>();
				if (inputFieldTMP && inputFieldTMP.isFocused)
					return;
#endif
			}

			m_ActionStarted = true;

			OnStarted();
		}

		private void OnInputPerformed(InputAction.CallbackContext obj)
		{
			var selected = m_PlayerContext.SelectedGameObject;

			if ((SkipHotkey & SkipHotkeyOption.NonTextSelectableFocused) != 0
				&& selected
				&& !selected.GetComponent<InputField>()
#if USE_TEXT_MESH_PRO
				&& !selected.GetComponent<TMPro.TMP_InputField>()
#endif
				)
				return;

			if ((SkipHotkey & SkipHotkeyOption.InputFieldTextFocused) != 0 && selected) {
				var inputField = selected.GetComponent<InputField>();
				if (inputField && inputField.isFocused)
					return;

#if USE_TEXT_MESH_PRO
				var inputFieldTMP = selected.GetComponent<TMPro.TMP_InputField>();
				if (inputFieldTMP && inputFieldTMP.isFocused)
					return;
#endif
			}

			m_ActionStarted = false;
			m_ActionPerformed = true;

			OnInvoke();
		}

		private void OnInputCancel(InputAction.CallbackContext obj)
		{
			var selected = m_PlayerContext.SelectedGameObject;

			if ((SkipHotkey & SkipHotkeyOption.NonTextSelectableFocused) != 0
				&& selected
				&& !selected.GetComponent<InputField>()
#if USE_TEXT_MESH_PRO
				&& !selected.GetComponent<TMPro.TMP_InputField>()
#endif
				)
				return;

			if ((SkipHotkey & SkipHotkeyOption.InputFieldTextFocused) != 0 && selected) {
				var inputField = selected.GetComponent<InputField>();
				if (inputField && inputField.isFocused)
					return;

#if USE_TEXT_MESH_PRO
				var inputFieldTMP = selected.GetComponent<TMPro.TMP_InputField>();
				if (inputFieldTMP && inputFieldTMP.isFocused)
					return;
#endif
			}

			m_ActionStarted = false;
			m_ActionPerformed = false;

			OnCancel();
		}

		protected virtual void OnStarted() { }
		protected abstract void OnInvoke();
		protected virtual void OnCancel() { }

		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			// Don't use m_SubscribedActions directly as the behaviour may not yet be enabled when this method is called.

			InputAction action = inputContext.FindActionFor(m_InputAction.name);
			if (action != null) {
				yield return action;
			}
		}

		protected virtual void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, m_InputAction, nameof(m_InputAction));
		}
	}
}

#endif