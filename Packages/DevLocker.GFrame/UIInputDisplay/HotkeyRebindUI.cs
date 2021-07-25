#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.UIInputDisplay
{
	/// <summary>
	/// Exposes API to rebind an InputAction.
	/// </summary>
	public class HotkeyRebindUI : MonoBehaviour
	{
		[Tooltip("The display of the hotkey - it will rebind it's InputAction.")]
		public HotkeyDisplayUI DisplayUI;

		public float WaitSecondsOnMatch = 0.1f;

		[Tooltip("Binding used for cancel. Examples:\n\"<Keyboard>/escape\" - ESC\n\"*/{Cancel}\" - any input considered as cancel (use with caution).")]
		[InputControlFIXED]
		public string CancelBinding;

		[InputControlFIXED]
		[NonReorderable]
		public string[] IncludeRebindsTo;

		[InputControlFIXED]
		[NonReorderable]
		public string[] ExcludeRebindsTo;

		public UnityEvent RebindFinished;
		public UnityEvent RebindReset;

		private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;

		void Awake()
		{
			if (DisplayUI == null) {
				DisplayUI = GetComponent<HotkeyDisplayUI>();
			}
		}


		[ContextMenu("Start Rebind")]
		public void StartRebind()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyRebindUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (m_RebindOperation != null) {
				m_RebindOperation.Dispose();
				m_RebindOperation = null;
			}

			InputAction action = context.FindActionFor(DisplayUI.Player.ToIndex(), DisplayUI.InputAction.name);

			if (DisplayUI.CurrentlyDisplayedData.Binding.id == Guid.Empty)
				return;

			// Actions need to be disabled in order to be re-bind.
			context.PushActionsState(this, true);

			m_RebindOperation = action.PerformInteractiveRebinding();
			m_RebindOperation.WithTargetBinding(DisplayUI.CurrentlyDisplayedData.BindingIndex);

			//m_RebindOperation.WithBindingGroup(DisplayUI.CurrentlyDisplayedData.ControlScheme); // Doesn't filter out groups?!
			//m_RebindOperation.WithExpectedControlType(/* Control type like "Key", "Button"*/);

			if (!string.IsNullOrEmpty(CancelBinding)) {
				m_RebindOperation.WithCancelingThrough(CancelBinding);
			}

			m_RebindOperation.OnMatchWaitForAnother(WaitSecondsOnMatch);

			foreach (var excluded in IncludeRebindsTo) {
				m_RebindOperation.WithControlsHavingToMatchPath(excluded);
			}

			foreach (var excluded in ExcludeRebindsTo) {
				m_RebindOperation.WithControlsExcluding(excluded);
			}

			m_RebindOperation.OnCancel(OnRebindCancel);
			m_RebindOperation.OnComplete(OnRebindComplete);

			m_RebindOperation.Start();
		}

		[ContextMenu("Cancel Rebind")]
		public void CancelRebind()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyRebindUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (m_RebindOperation != null) {
				m_RebindOperation.Cancel();
			}
		}

		[ContextMenu("Reset Bind")]
		public void ResetBind()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(HotkeyRebindUI)} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (m_RebindOperation != null) {
				m_RebindOperation.Cancel();
				// Will be disposed by the OnCancel hook.
			}

			InputAction action = context.FindActionFor(DisplayUI.Player.ToIndex(), DisplayUI.InputAction.name);
			InputActionRebindingExtensions.RemoveBindingOverride(action, DisplayUI.CurrentlyDisplayedData.BindingIndex);

			DisplayUI.RefreshDisplay();
			RebindReset.Invoke();
		}

		private void OnRebindComplete(InputActionRebindingExtensions.RebindingOperation operation)
		{
			Debug.Assert(m_RebindOperation == operation);

			if (m_RebindOperation == null)
				return;

			m_RebindOperation.Dispose();
			m_RebindOperation = null;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			context?.PopActionsState(this);

			DisplayUI.RefreshDisplay();
			RebindFinished.Invoke();
		}

		private void OnRebindCancel(InputActionRebindingExtensions.RebindingOperation operation)
		{
			Debug.Assert(m_RebindOperation == operation);

			if (m_RebindOperation == null)
				return;

			m_RebindOperation.Dispose();
			m_RebindOperation = null;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			context?.PopActionsState(this);

			DisplayUI.RefreshDisplay();
			RebindFinished.Invoke();
		}

		private void OnDestroy()
		{
			if (m_RebindOperation != null) {
				m_RebindOperation.Dispose();
				m_RebindOperation = null;
			}
		}
	}

}
#endif