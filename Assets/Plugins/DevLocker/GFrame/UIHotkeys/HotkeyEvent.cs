#if USE_INPUT_SYSTEM

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.UI
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyEvent : MonoBehaviour, IHotkeyElement
	{
		[SerializeField]
		private InputActionReference m_InputAction;

		[SerializeField]
		private UnityEvent m_OnAction;

		void OnEnable()
		{
			var controls = (LevelsManager.Instance.GameContext as IInputActionsProvider)?.Controls;

			if (controls == null) {
				Debug.LogWarning($"{nameof(HotkeyEvent)} {name} can't be used if Unity Input System is not provided.", this);
				return;
			}

			controls.FindAction(m_InputAction.name).performed += OnInputAction;
		}

		void OnDisable()
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var controls = (LevelsManager.Instance.GameContext as IInputActionsProvider)?.Controls;

			if (controls == null) {
				return;
			}

			controls.FindAction(m_InputAction.name).performed -= OnInputAction;
		}

		private void OnInputAction(InputAction.CallbackContext obj)
		{
			m_OnAction.Invoke();
		}
	}
}

#endif