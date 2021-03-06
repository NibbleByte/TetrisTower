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

		bool CheckIfAnyActionIsEnabled();
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
		[Tooltip("When scope gets enabled this will activate the input actions (hotkeys) that are used by the scope elements under it.\nOn disabling it will deactivate the actions.\nNOTE: to avoid input conflicts, don't control the same actions from the code.")]
		public bool EnableUsedInputActions = true;
		[Tooltip("Use this for modal windows to suppress background hotkeys.\n\nPushes a new input state in the stack.\nOn deactivating, will pop this state and restore the previous one.\nThe only enabled actions will be the used ones by (under) this scope.")]
		public bool PushInputStack = false;
		[Tooltip("Enable the UI actions with the scope ones, after pushing the new input state.")]
		public bool IncludeUIActions = true;
#endif

		public static IReadOnlyCollection<UIScope> ActiveScopes => m_ActiveScopes;
		private static UIScope[] m_ActiveScopes = Array.Empty<UIScope>();

		private static List<UIScope> s_Scopes = new List<UIScope>();

		private List<IScopeElement> m_ScopeElements = new List<IScopeElement>();
		private List<UIScope> m_DirectChildScopes = new List<UIScope>();

		// Switching scopes may trigger user code that may switch scopes indirectly, while already doing so.
		// Any such change will be pushed to a queue and applied later on.
		private static bool s_ChangingActiveScopes = false;
		private static Queue<KeyValuePair<UIScope, bool>> s_PendingScopeChanges = new Queue<KeyValuePair<UIScope, bool>>();


		protected virtual void Awake()
		{
			ScanForChildScopeElements();
		}

		void OnEnable()
		{
			if (s_ChangingActiveScopes) {
				s_PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, true));
				return;
			}

			// Child scope was active, but this one was disabled. The user just enabled me.
			// Re-insert me (us) to the collections keeping the correct order.
			if (m_ActiveScopes.Length > 0 && m_ActiveScopes.Last().transform.IsChildOf(transform)) {

				// That would include me, freshly enabled.
				UIScope[] nextScopes = CollectScopes(m_ActiveScopes.Last());

				foreach (UIScope scope in nextScopes) {
					s_Scopes.Remove(scope);
					s_Scopes.Add(scope);
				}

				SwitchActiveScopes(ref m_ActiveScopes, nextScopes);

			} else {

				// OnEnabled() order of execution is undefined - sometimes parent invoked first, sometimes the children.
				// Ensure that collections don't have any duplicates and are filled in the right order - parent to child.
				UIScope[] nextScopes = CollectScopes(this);

				foreach (UIScope scope in nextScopes) {
					if (!s_Scopes.Contains(scope)) {
						s_Scopes.Add(scope);
					}
				}

				SwitchActiveScopes(ref m_ActiveScopes, nextScopes);
			}
		}

		void OnDisable()
		{
			if (s_ChangingActiveScopes) {
				s_PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, false));
				return;
			}

			UIScope nextDeepestScope = null;
			bool wasActive = false;

			// HACK: On turning off the game OnDisable() gets called and LevelsManager.Instance may get destroyed before that.
			if (Array.IndexOf(m_ActiveScopes, this) != -1 && LevelsManager.Instance) {

				// Try keep the current lowest scope as the target one.
				if (this != m_ActiveScopes.Last()) {
					nextDeepestScope = m_ActiveScopes.Last();
				}

				wasActive = true;
			}

			s_Scopes.Remove(this);

			if (wasActive && LevelsManager.Instance) {
				// Pick the next lowest (latest) child registered.
				nextDeepestScope = nextDeepestScope ?? s_Scopes.LastOrDefault();
				UIScope[] nextScopes = nextDeepestScope
					? CollectScopes(nextDeepestScope)
					: Array.Empty<UIScope>()
					;

				SwitchActiveScopes(ref m_ActiveScopes, nextScopes);
			}
		}

		/// <summary>
		/// Force selected scope to be active, instead of the last enabled.
		/// </summary>
		/// <param name="scope"></param>
		[ContextMenu("Force activate scope")]
		public void ForceActiveScope()
		{
			if (s_ChangingActiveScopes) {
				s_PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, true));
				return;
			}

			// That would be weird.
			if (!gameObject.activeInHierarchy) {
				Debug.LogWarning($"Trying to force activate UIScope {name}, but it is not active in the hierarchy. Abort!", this);
				return;
			}

			UIScope[] nextScopes = CollectScopes(this);

			SwitchActiveScopes(ref m_ActiveScopes, nextScopes);
		}

		public static bool IsScopeActive(UIScope scope) => m_ActiveScopes.Contains(scope);

		/// <summary>
		/// Call this if you changed your UI hierarchy and expect added or removed scope elements.
		/// </summary>
		[ContextMenu("Rescan for child scope elements")]
		public void ScanForChildScopeElements()
		{
			m_ScopeElements.Clear();
			m_DirectChildScopes.Clear();
			ScanForChildScopeElements(this, transform, m_ScopeElements, m_DirectChildScopes);

			if (Array.IndexOf(m_ActiveScopes, this) != -1) {
				var lastActive = m_ActiveScopes.Last();

				if (s_ChangingActiveScopes) {
					s_PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(lastActive, true));
					return;
				}

				// Force full re-initialization of all the scopes including this one.
				SwitchActiveScopes(ref m_ActiveScopes, new UIScope[0]);
				lastActive.ForceActiveScope();

			} else {
				foreach(IScopeElement scopeElement in m_ScopeElements) {
					scopeElement.enabled = false;
				}
			}
		}

		internal static void ScanForChildScopeElements(UIScope parentScope, Transform transform, List<IScopeElement> scopeElements, List<UIScope> directChildScopes)
		{
			var scope = transform.GetComponent<UIScope>();
			// Another scope begins, it will handle its own child hotkey elements.
			if (scope && parentScope != scope) {
				directChildScopes.Add(scope);
				return;
			}

			scopeElements.AddRange(transform.GetComponents<IScopeElement>());

			foreach(Transform child in transform) {
				ScanForChildScopeElements(parentScope, child, scopeElements, directChildScopes);
			}
		}

		protected static UIScope[] CollectScopes(Component target)
		{
			return target
				.GetComponentsInParent<UIScope>()
				.Reverse()
				.Where(s => s.enabled)
				.ToArray();
		}

		protected static void SwitchActiveScopes(ref UIScope[] prevScopes, UIScope[] nextScopes)
		{
			// Switching scopes may trigger user code that may switch scopes indirectly, while already doing so.
			// Any such change will be pushed to a queue and applied later on.
			// TODO: This was never really tested.
			s_ChangingActiveScopes = true;

			try {
				// Reversed order, just in case.
				foreach (UIScope scope in prevScopes.Reverse()) {
					if (!nextScopes.Contains(scope)) {
						scope.SetScopeState(false);
					}
				}

				foreach (UIScope scope in nextScopes) {
					if (!prevScopes.Contains(scope)) {
						scope.SetScopeState(true);
					}
				}
			}

			finally {
				prevScopes = nextScopes;
				s_ChangingActiveScopes = false;
			}

			while(s_PendingScopeChanges.Count > 0) {
				var scopeChange = s_PendingScopeChanges.Dequeue();

				if (scopeChange.Value) {
					scopeChange.Key.OnEnable();
				} else {
					scopeChange.Key.OnDisable();
				}
			}
		}

		protected virtual void SetScopeState(bool active)
		{
			// If this scope isn't still initialized, do it now, or no elements will be enabled.
			// This happens when child scope tries to activate the parent scope for the first time, while the parent was still inactive.
			if (m_ScopeElements.Count == 0) {
				ScanForChildScopeElements();
			}

			foreach(IScopeElement scopeElement in m_ScopeElements) {
				scopeElement.enabled = active;
			}

			ProcessInput(active);
		}

		protected void ProcessInput(bool active)
		{
#if USE_INPUT_SYSTEM
			var context = (LevelsManager.Instance.GameContext as Input.IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(UIScope)} {name} can't be used if Unity Input System is not provided.", this);
				return;
			}

			// Pushing input on stack will reset the actions anyway.
			if (ResetAllActionsOnEnable && active && !PushInputStack) {
				context.ResetAllEnabledActions();
			}

			if (active) {

				if (PushInputStack) {
					context.PushActionsState(this);

					if (IncludeUIActions) {
						foreach (var action in context.GetUIActions()) {
							action.Enable();
						}
					}
				}

				// Because the PushInputStack will have disabled all input actions.
				if (EnableUsedInputActions || PushInputStack) {
					foreach (var action in m_ScopeElements
						.OfType<IHotkeyWithInputAction>()
						.SelectMany(element => element.GetUsedActions())
						.Distinct()) {

						// MessageBox has multiple buttons with the same hotkey, but only one is active.
						if (action.enabled) {
							Debug.LogWarning($"{nameof(UIScope)} {name} is enabling action {action.name} that is already enabled. This is a sign of an input conflict!", this);
						}
						action.Enable();
					}
				}

			} else {

				if (PushInputStack) {
					context.PopActionsState(this);

				} else if (EnableUsedInputActions) {

					foreach (IHotkeyWithInputAction hotkeyElement in m_ScopeElements.OfType<IHotkeyWithInputAction>()) {
						foreach (var action in hotkeyElement.GetUsedActions()) {
							// This can often be a valid case since the code may push a new state in the input stack, resetting all the actions, before changing the UIScopes.
							//if (!action.enabled) {
							//	Debug.LogWarning($"{nameof(UIScope)} {name} is disabling action {action.name} that is already disabled. This is a sign of an input conflict!", this);
							//}
							action.Disable();
						}
					}

				}
			}
#endif
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(UIScope), true)]
	internal class UIScopeEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var uiScope = (UIScope)target;

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("ResetAllActionsOnEnable"));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableUsedInputActions"));
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("PushInputStack"));
			if (uiScope.PushInputStack) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("IncludeUIActions"));
			}

			serializedObject.ApplyModifiedProperties();


			var scopeElements = new List<IScopeElement>();
			var directChildScopes = new List<UIScope>();
			UIScope.ScanForChildScopeElements(uiScope, uiScope.transform, scopeElements, directChildScopes);


			UnityEditor.EditorGUILayout.Space();
			UnityEditor.EditorGUILayout.LabelField("Controlled Elements:", UnityEditor.EditorStyles.boldLabel);

			foreach(var element in scopeElements) {
				UnityEditor.EditorGUILayout.BeginHorizontal();
				UnityEditor.EditorGUILayout.ObjectField(element as UnityEngine.Object, typeof(IScopeElement), true);

#if USE_INPUT_SYSTEM
				if (element is IHotkeyWithInputAction hotkeyElement) {

					var prevColor = GUI.color;

					bool actionsActive = uiScope.enabled && uiScope.gameObject.activeInHierarchy && hotkeyElement.CheckIfAnyActionIsEnabled();
					string activeStr = actionsActive ? "Active" : "Inactive";
					GUI.color = actionsActive ? Color.green : Color.red;

					GUILayout.Label(new GUIContent(activeStr, "Are the hotkey input actions active or not?"), GUILayout.ExpandWidth(false));
					GUI.color = prevColor;
				}
#endif
				UnityEditor.EditorGUILayout.EndHorizontal();
			}
		}
	}
#endif
}