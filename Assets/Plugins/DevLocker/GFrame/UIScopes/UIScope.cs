using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevLocker.GFrame.UIScope
{
	/// <summary>
	/// Marks scope elements to be controlled by the UIScope.
	/// </summary>
	public interface IScopeElement
	{
		bool enabled { get; set; }
	}

#if USE_INPUT_SYSTEM
	/// <summary>
	/// Used with Unity InputSystem for pushing and popping states in the InputActionsStack.
	/// </summary>
	public interface IHotkeyWithInputAction
	{
		IEnumerable<UnityEngine.InputSystem.InputAction> GetUsedActions();
	}
#endif

	/// <summary>
	/// Used to skip hotkeys in some cases.
	/// </summary>
	[Flags]
	public enum SkipHotkeyOption
	{
		InputFieldTextFocused = 1 << 0,
		NonTextSelectableFocused = 1 << 1,
	}

	/// <summary>
	/// When working with more hotkeys, selections, etc. on screen, some conflicts may arise.
	/// For example:
	/// > "Back" hotkey element is attached on the displayed menu and another "Back" hotkey attached to the "Yes/No" pop up, displayed on top.
	/// > The usual solution is to invoke only the last enabled hotkey instead of all of them.
	/// UIScope groups all child scope elements into a single group. The last enabled UIScope is active, while the rest will be disabled.
	/// </summary>
	[SelectionBase]
	public class UIScope : MonoBehaviour
	{
#if USE_INPUT_SYSTEM
		[Tooltip("Reset all input actions.\nThis will interrupt their progress and any gesture, drag, sequence will be canceled.")]
		public bool ResetAllActionsOnEnable = true;

		[Space]
		[Tooltip("Push a new input state in the stack.\nOn deactivating, will pop this state and restore the previous one.\nThe only enabled actions will be the used ones by (under) this scope.")]
		public bool PushInputStack = false;
		[Tooltip("Enable the UI actions with the scope ones, after pushing the new input state.")]
		public bool IncludeUIActions = true;
#endif

		public static UIScope ActiveScope { get; private set; }

		private static List<UIScope> s_Scopes = new List<UIScope>();

		private List<IScopeElement> m_ScopeElements = new List<IScopeElement>();

		protected virtual void Awake()
		{
			ScanForChildScopeElements();
		}

		void OnEnable()
		{
			ActiveScope?.SetScopeState(false);

			s_Scopes.Add(this);

			ActiveScope = this;
			ActiveScope.SetScopeState(true);
		}

		void OnDisable()
		{
			bool wasActive = false;
			if (ActiveScope == this) {
				ActiveScope.SetScopeState(false);
				wasActive = true;
			}

			s_Scopes.Remove(this);

			if (wasActive) {
				ActiveScope = s_Scopes.Count > 0 ? s_Scopes[s_Scopes.Count - 1] : null;
				ActiveScope?.SetScopeState(true);
			}
		}

		/// <summary>
		/// Force selected scope to be active, instead of the last enabled.
		/// </summary>
		/// <param name="scope"></param>
		public void ForceActiveScope()
		{
			ActiveScope?.SetScopeState(false);

			ActiveScope = this;
			ActiveScope.SetScopeState(true);
		}

		/// <summary>
		/// Call this if you changed your UI hierarchy and expect added or removed scope elements.
		/// </summary>
		public void ScanForChildScopeElements()
		{
			m_ScopeElements.Clear();
			ScanForChildScopeElements(this, transform, m_ScopeElements);

			if (ActiveScope == this) {
				ActiveScope.SetScopeState(true);

			} else if (ActiveScope != null) {
				SetScopeState(false);
			}
		}

		internal static void ScanForChildScopeElements(UIScope parentScope, Transform transform, List<IScopeElement> scopeElements)
		{
			var scope = transform.GetComponent<UIScope>();
			// Another scope begins, it will handle its own child hotkey elements.
			if (scope && parentScope != scope)
				return;

			scopeElements.AddRange(transform.GetComponents<IScopeElement>());

			foreach(Transform child in transform) {
				ScanForChildScopeElements(parentScope, child, scopeElements);
			}
		}

		protected virtual void SetScopeState(bool active)
		{
			foreach(IScopeElement scopeElement in m_ScopeElements) {
				scopeElement.enabled = active;
			}

#if USE_INPUT_SYSTEM
			// Pushing input on stack will reset the actions anyway.
			if (ResetAllActionsOnEnable && active && !PushInputStack) {
				var context = (LevelsManager.Instance.GameContext as Input.IInputContextProvider)?.InputContext;

				if (context == null) {
					Debug.LogWarning($"{nameof(UIScope)} {name} can't be used if Unity Input System is not provided.", this);
					return;
				}

				context.ResetAllActions();
			}

			if (PushInputStack) {

				var context = (LevelsManager.Instance.GameContext as Input.IInputContextProvider)?.InputContext;

				if (context == null) {
					Debug.LogWarning($"{nameof(UIScope)} {name} can't be used if Unity Input System is not provided.", this);
					return;
				}

				if (active) {

					context.PushActionsState(this);

					foreach (IHotkeyWithInputAction hotkeyElement in m_ScopeElements.OfType<IHotkeyWithInputAction>()) {
						foreach (var action in hotkeyElement.GetUsedActions()) {
							action.Enable();
						}
					}

					if (IncludeUIActions) {
						foreach(var action in context.GetUIActions()) {
							action.Enable();
						}
					}

				} else {
					context.PopActionsState(this);
				}
			}
#endif
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(UIScope), true)]
	public class BaseDefEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var uiScope = (UIScope)target;

			var scopeElements = new List<IScopeElement>();
			UIScope.ScanForChildScopeElements(uiScope, uiScope.transform, scopeElements);


			UnityEditor.EditorGUILayout.Space();
			UnityEditor.EditorGUILayout.LabelField("Controlled Elements:", UnityEditor.EditorStyles.boldLabel);

			foreach(var element in scopeElements) {
				UnityEditor.EditorGUILayout.ObjectField(element as UnityEngine.Object, typeof(IScopeElement), true);
			}
		}
	}
#endif
}