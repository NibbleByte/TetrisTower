#if USE_INPUT_SYSTEM

using DevLocker.GFrame.Input;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
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
		[Tooltip("Which player should this hotkey be listened to?\nIf unsure or for single player games, leave MasterPlayer.")]
		public PlayerIndex Player = PlayerIndex.AnyPlayer;

		[Tooltip("Skip the hotkey based on the selected condition.")]
		[Utils.EnumMask]
		public SkipHotkeyOption SkipHotkey;

		[SerializeField]
		protected InputActionReference m_InputAction;

		protected List<InputAction> m_SubscribedActions = new List<InputAction>();

		protected virtual void OnEnable()
		{
			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyButtonScopeElement)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			InputContextManager.InputContext.PlayersChanged += OnPlayersChanged;

			foreach(InputAction action in GetUsedActions()) {
				m_SubscribedActions.Add(action);
				action.performed += OnInputAction;
			}
		}

		protected virtual void OnDisable()
		{
			if (InputContextManager.InputContext == null)
				return;

			InputContextManager.InputContext.PlayersChanged -= OnPlayersChanged;

			foreach (InputAction action in m_SubscribedActions) {
				action.performed -= OnInputAction;
			}

			m_SubscribedActions.Clear();
		}

		protected virtual void OnPlayersChanged()
		{
			OnDisable();
			OnEnable();
		}

		private void OnInputAction(InputAction.CallbackContext obj)
		{
			var selected = EventSystem.current.currentSelectedGameObject;

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

			OnInvoke();
		}

		protected abstract void OnInvoke();

		public IEnumerable<InputAction> GetUsedActions()
		{
			if (InputContextManager.InputContext == null) {
				Debug.LogWarning($"{nameof(HotkeyButtonScopeElement)} button {name} can't be used if Unity Input System is not provided.", this);
				Enumerable.Empty<InputAction>();
			}

			// Don't use m_SubscribedActions directly as the behaviour may not yet be enabled when this method is called.

			if (Player == PlayerIndex.AnyPlayer) {
				foreach (InputAction action in InputContextManager.InputContext.FindActionsForAllPlayers(m_InputAction.name)) {
					yield return action;
				}
			} else {
				InputAction action = InputContextManager.InputContext.FindActionFor(Player, m_InputAction.name);
				if (action != null) {
					yield return action;
				}
			}
		}

		public bool CheckIfAnyActionIsEnabled()
		{
			if (InputContextManager.InputContext == null)
				return false;

			foreach(InputAction action in GetUsedActions()) {
				if (action.enabled)
					return true;
			}

			return false;
		}

		protected virtual void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, m_InputAction, nameof(m_InputAction));
		}
	}
}

#endif