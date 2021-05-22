#if USE_INPUT_SYSTEM

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.UIHotkeys
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeyEvent : MonoBehaviour, IHotkeyElement, IHotkeyWithInputAction
	{
		[SerializeField]
		private InputActionReference m_InputAction;

		[SerializeField]
		private UnityEvent m_OnAction;

		void OnEnable()
		{
			var context = (LevelsManager.Instance.GameContext as IInputActionsContext);

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyEvent)} {name} can't be used if Unity Input System is not provided.", this);
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
			m_OnAction.Invoke();
		}

		public IEnumerable<InputAction> GetUsedActions()
		{
			var context = (LevelsManager.Instance.GameContext as IInputActionsContext);

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyButton)} button {name} can't be used if Unity Input System is not provided.", this);
				yield break;
			}

			yield return context.FindAction(m_InputAction.name);
		}
	}
}

#endif