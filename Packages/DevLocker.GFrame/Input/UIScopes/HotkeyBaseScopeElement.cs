#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Base class for hotkey scope elements (that use Unity's Input System).
	/// Note that this component will enable the input action and it needs to stay enabled to be invoked.
	/// </summary>
	public abstract class HotkeyBaseScopeElement : MonoBehaviour, IScopeElement, IHotkeysWithInputActions, IWritableHotkeyInputActionReference
	{
		[Tooltip("Skip the hotkey based on the selected condition.")]
		[Utils.EnumMask]
		[HideInInspector]	// Draw manually in the editor.
		public SkipHotkeyOption SkipHotkey = SkipHotkeyOption.InputFieldTextFocused;

		[HideInInspector]	// Draw manually in the editor.
		[SerializeField]
		protected InputActionReference m_InputAction;
		public InputActionReference InputAction => m_InputAction;

		protected bool m_ActionStarted { get; private set; } = false;
		protected bool m_ActionPerformed { get; private set; } = false;

		private Coroutine m_CancelCheckAfterPeformCrt;

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

				OnContextReady();

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		protected virtual void OnContextReady()
		{

		}

		protected virtual void OnDestroy()
		{

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

		private void OnInputStarted(InputAction.CallbackContext context)
		{
			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = true;

			OnStart(context);
		}

		private void OnInputPerformed(InputAction.CallbackContext context)
		{
			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = true;

			if (m_CancelCheckAfterPeformCrt != null) {
				StopCoroutine(m_CancelCheckAfterPeformCrt);
			}

			// Buttons only, values get canceled right away after each value change.
			if (context.action.type == InputActionType.Button) {
				m_CancelCheckAfterPeformCrt = StartCoroutine(CancelCheckAfterPeform(context));
			}

			OnInvoke(context);
		}

		private void OnInputCancel(InputAction.CallbackContext context)
		{
			if (m_CancelCheckAfterPeformCrt != null) {
				StopCoroutine(m_CancelCheckAfterPeformCrt);
				m_CancelCheckAfterPeformCrt = null;
			}

			if (PlayerContextUtils.ShouldSkipHotkey(m_PlayerContext, SkipHotkey))
				return;

			if (!Utils.UIUtils.IsClickable(gameObject))
				return;

			m_ActionStarted = false;
			m_ActionPerformed = false;

			OnCancel(context);
		}

		/// <summary>
		/// Checks if cancel happened right after perform. In some cases, cancel event is skipped.
		/// If cancel event is triggered, the coroutine will be cleared.
		/// (Buttons only, Values types get canceled right away after each value change.)
		/// Example: Have two actions with the same bindings, one with a tap and the other with hold interactions.
		///			 If you press and release quickly for a tap, you'll get tap-perform, hold-cancel, but no tap-cancel.
		/// </summary>
		private IEnumerator CancelCheckAfterPeform(InputAction.CallbackContext context)
		{
			yield return new WaitForEndOfFrame();

			if (!m_PlayerContext.IsActive)
				yield break;

			InputAction action = m_PlayerContext.InputContext.FindActionFor(m_InputAction.name);
			if (action.phase != InputActionPhase.Performed) {
				// Context is the same in all the events - it keeps reference to the state. I think.
				OnInputCancel(context);
			}
		}

		protected virtual void OnStart(InputAction.CallbackContext context) { }
		protected abstract void OnInvoke(InputAction.CallbackContext context);
		protected virtual void OnCancel(InputAction.CallbackContext context) { }

		public IEnumerable<InputAction> GetUsedActions(IInputContext inputContext)
		{
			if (m_InputAction == null)
				yield break;

#if UNITY_EDITOR
			// For editor purposes.
			if (inputContext == null) {
				yield return m_InputAction;
				yield break;
			}
#endif

			InputAction action = inputContext.FindActionFor(m_InputAction.name);
			if (action != null) {
				yield return action;
			}
		}

		/// <summary>
		/// Set input action. Will rebind it properly.
		/// </summary>
		public void SetInputAction(InputActionReference inputActionReference)
		{
			bool wasEnabled = Application.isPlaying && isActiveAndEnabled;
			if (wasEnabled) {
				OnDisable();
			}

			m_InputAction = inputActionReference;

			if (wasEnabled) {
				OnEnable();
			}
		}

		/// <summary>
		/// Call this after you changed the current InputAction and you want to apply it to all child indicators etc.
		/// </summary>
		public void ApplyInputActionToChildren()
		{
			var children = GetComponentsInChildren<IWritableHotkeyInputActionReference>(true);
			foreach (IWritableHotkeyInputActionReference child in children) {
				if (ReferenceEquals(child, this))
					continue;

				child.SetInputAction(InputAction);

#if UNITY_EDITOR
				if (!Application.isPlaying) {
					EditorUtility.SetDirty((MonoBehaviour)child);
				}
#endif
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
			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyBaseScopeElement.SkipHotkey)));

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_" + nameof(HotkeyBaseScopeElement.InputAction)));

			if (EditorGUI.EndChangeCheck()) {
				m_WasChanged = true;
				serializedObject.ApplyModifiedProperties();
			}

			if (m_WasChanged) {

				EditorGUILayout.HelpBox("Change was detected. Do you want to apply the hotkey to all the child elements so they are the same?", MessageType.Warning);

				EditorGUILayout.BeginHorizontal();

				Color prevColor = GUI.backgroundColor;
				GUI.backgroundColor = Color.yellow;

				if (GUILayout.Button("Apply InputAction to Children")) {
					m_WasChanged = false;

					foreach (var hotkeyElement in targets.OfType<HotkeyBaseScopeElement>()) {
						hotkeyElement.ApplyInputActionToChildren();
					}
				}

				GUI.backgroundColor = prevColor;

				if (GUILayout.Button("X", GUILayout.Width(20))) {
					m_WasChanged = false;
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();


			EditorGUI.BeginChangeCheck();

			DrawPropertiesExcluding(serializedObject, "m_Script");

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif

}

#endif