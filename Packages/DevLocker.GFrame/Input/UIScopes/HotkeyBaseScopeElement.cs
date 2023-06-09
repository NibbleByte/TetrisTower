#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Base class for hotkey scope elements (that use Unity's Input System).
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public abstract class HotkeyBaseScopeElement : MonoBehaviour, IScopeElement, IHotkeysWithInputActions, IWritableHotkeyInputAction
	{
		[Tooltip("Skip the hotkey based on the selected condition.")]
		[Utils.EnumMask]
		public SkipHotkeyOption SkipHotkey;

		[SerializeField]
		protected InputActionReference m_InputAction;
		public InputActionReference InputAction => m_InputAction;

		protected bool m_ActionStarted { get; private set; } = false;
		protected bool m_ActionPerformed { get; private set; } = false;

		protected InputEnabler m_InputEnabler;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected bool m_HasInitialized = false;

		protected virtual void Reset()
		{
			// Let scopes do the enabling or else you'll get warnings for hotkey conflicts for multiple scopes with the same hotkey on screen.
			enabled = false;
		}

		protected virtual void Awake()
		{
			m_InputEnabler = new InputEnabler(this);

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
				Debug.LogWarning($"[Input] {GetType().Name} button {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			foreach(InputAction action in GetUsedActions(m_PlayerContext.InputContext)) {
				action.started += OnInputStarted;
				action.performed += OnInputPerformed;
				action.canceled += OnInputCancel;

				m_InputEnabler.Enable(action);
			}
		}

		protected virtual void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			// Not needed. Better unsubscribe even if context got disposed.
			//if (m_PlayerContext.InputContext == null)
			//	return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			foreach (InputAction action in m_InputEnabler.ToList()) {
				action.started -= OnInputStarted;
				action.performed -= OnInputPerformed;
				action.canceled -= OnInputCancel;
				m_InputEnabler.Disable(action);
			}
		}

		private void OnInputStarted(InputAction.CallbackContext obj)
		{
			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = true;

			OnStarted();
		}

		private void OnInputPerformed(InputAction.CallbackContext obj)
		{
			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = true;

			OnInvoke();
		}

		private void OnInputCancel(InputAction.CallbackContext obj)
		{
			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			OnCancel();
		}

		protected virtual void OnStarted() { }
		protected abstract void OnInvoke();
		protected virtual void OnCancel() { }

		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (m_InputAction == null)
				yield break;

			InputAction action = inputContext.FindActionFor(m_InputAction.name);
			if (action != null) {
				yield return action;
			}
		}

		/// <summary>
		/// Set input action. Will rebind it properly.
		/// </summary>
		public void SetInputAction(InputAction inputAction)
		{
			bool wasEnabled = enabled;
			if (wasEnabled) {
				OnDisable();
			}

			m_InputAction = InputActionReference.Create(inputAction);

			if (wasEnabled) {
				OnEnable();
			}
		}

		protected virtual void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, m_InputAction, nameof(m_InputAction));

			// Check the Reset() message.
			if (!Application.isPlaying && enabled) {
				enabled = false;
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HotkeyBaseScopeElement), true)]
	[CanEditMultipleObjects]
	internal class HotkeyBaseScopeElementEditor : Editor
	{
		private bool m_WasChanged = false;

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			DrawDefaultInspector();

			if (EditorGUI.EndChangeCheck()) {
				m_WasChanged = true;
			}

			if (m_WasChanged) {

				EditorGUILayout.HelpBox("Change was detected. Do you want to apply the hotkey to all the child elements so they are the same?", MessageType.Warning);

				EditorGUILayout.BeginHorizontal();

				Color prevColor = GUI.backgroundColor;
				GUI.backgroundColor = Color.yellow;

				if (GUILayout.Button("Apply InputAction to Children")) {
					m_WasChanged = false;

					foreach (var hotkeyElement in targets.OfType<HotkeyBaseScopeElement>()) {
						var children = hotkeyElement.GetComponentsInChildren<IWritableHotkeyInputAction>();
						foreach (IWritableHotkeyInputAction child in children) {
							if (ReferenceEquals(child, hotkeyElement))
								continue;

							child.SetInputAction(hotkeyElement.InputAction);
							EditorUtility.SetDirty((MonoBehaviour)child);
						}
					}
				}

				GUI.backgroundColor = prevColor;

				if (GUILayout.Button("X", GUILayout.Width(20))) {
					m_WasChanged = false;
				}

				EditorGUILayout.EndHorizontal();

			}
		}
	}
#endif

}

#endif