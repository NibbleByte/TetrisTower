#if USE_INPUT_SYSTEM

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.UIScope
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyEventScopeElement : MonoBehaviour, IScopeElement, IHotkeyWithInputAction
	{

		[Tooltip("Skip the hotkey based on the selected condition.")]
		public HotkeyButtonScopeElement.SkipHotkeyOption SkipHotkey = HotkeyButtonScopeElement.SkipHotkeyOption.Never;

		[SerializeField]
		private InputActionReference m_InputAction;

		[SerializeField]
		private UnityEvent m_OnAction;

		void OnEnable()
		{
			var context = (LevelsManager.Instance.GameContext as IInputActionsContext);

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyEventScopeElement)} {name} can't be used if Unity Input System is not provided.", this);
				return;
			}

			context.FindAction(m_InputAction.name).performed += OnInputAction;
		}

		void OnDisable()
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var context = (LevelsManager.Instance.GameContext as IInputActionsContext);

			if (context == null) {
				return;
			}

			context.FindAction(m_InputAction.name).performed -= OnInputAction;
		}

		private void OnInputAction(InputAction.CallbackContext obj)
		{
			if (SkipHotkey == HotkeyButtonScopeElement.SkipHotkeyOption.AnySelectableFocused && EventSystem.current.currentSelectedGameObject)
				return;

			if (SkipHotkey == HotkeyButtonScopeElement.SkipHotkeyOption.EnteringText && EventSystem.current.currentSelectedGameObject) {
				var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
				if (inputField && inputField.isFocused)
					return;

#if USE_TEXT_MESH_PRO
				var inputFieldTMP = EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
				if (inputFieldTMP && inputFieldTMP.isFocused)
					return;
#endif
			}

			m_OnAction.Invoke();
		}

		public IEnumerable<InputAction> GetUsedActions()
		{
			var context = (LevelsManager.Instance.GameContext as IInputActionsContext);

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyButtonScopeElement)} button {name} can't be used if Unity Input System is not provided.", this);
				yield break;
			}

			yield return context.FindAction(m_InputAction.name);
		}
	}
}

#endif