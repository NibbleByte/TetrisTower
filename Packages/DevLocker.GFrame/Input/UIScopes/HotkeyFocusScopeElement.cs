#if USE_INPUT_SYSTEM

using System;
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
	/// Calls UnityEvent on specified InputAction.
	/// Note that this component will enable the input action and it needs to stay enabled to be invoked.
	/// </summary>
	public class HotkeyFocusScopeElement : HotkeyBaseScopeElement
	{
		public enum FocusPolicyType
		{
			FocusScope = 0,
			FocusScopeWithHighestDepth = 2,
			FocusPreviousScope = 5,
			FocusParentScope = 10,
			FocusFirstEnabledScopeFromList = 15,
		}

		[HideInInspector]
		[Tooltip("When hotkey is pressed, scope will get focused until hotkey gets released. When that happens, previous scope will be focused.")]
		public bool FocusWhilePressed;

		[HideInInspector]
		public FocusPolicyType FocusPolicy;

		[HideInInspector]
		public List<UIScope> FocusScopes = new List<UIScope>();

		private UIScope m_PrevScopeWhenPressed;
		private UIScope m_TargetScopeWhenPressed;

		protected override void OnEnable()
		{
			base.OnEnable();

			UIScope.ScopeFocused += OnScopeFocused;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			UIScope.ScopeFocused -= OnScopeFocused;
			m_PrevScopeWhenPressed = null;
			m_TargetScopeWhenPressed = null;

		}

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			if (!FocusWhilePressed) {
				UIScope nextScope = GetTargetScopeToFocus();

				if (nextScope) {
					if (nextScope.IsFocused || !nextScope.isActiveAndEnabled)
						return;

					nextScope.Focus();
				} else {
					Debug.LogWarning($"[Input] No scope to focus for \"{name}\" - \"{m_InputAction}\"", this);
				}

			} else if (m_PrevScopeWhenPressed == null) {
				m_PrevScopeWhenPressed = UIScope.FocusedScope(m_PlayerContext.GetRootObject());
				m_TargetScopeWhenPressed = GetTargetScopeToFocus();

				if (m_TargetScopeWhenPressed) {
					// If target scope is already focused for some reason, cancel the operation as we don't have an actual previous scope to return to.
					if (m_PrevScopeWhenPressed == m_TargetScopeWhenPressed || !m_TargetScopeWhenPressed.isActiveAndEnabled) {
						m_TargetScopeWhenPressed = null;
						m_PrevScopeWhenPressed = null;
						return;
					}

					if (m_TargetScopeWhenPressed.ResetAllActionsOnEnable) {
						Debug.LogError($"Focusing scope \"{m_TargetScopeWhenPressed.name}\" by holding \"{m_InputAction.name}\", but it will reset all actions on enable! Disable this setting!", m_TargetScopeWhenPressed);
					}

					if (!m_TargetScopeWhenPressed.IsFocused) {
						m_TargetScopeWhenPressed.Focus();
					}
				} else {
					Debug.LogWarning($"[Input] No scope to focus for \"{name}\" - \"{m_InputAction}\"", this);
					m_PrevScopeWhenPressed = null;
				}
			}
		}

		protected override void OnCancel(InputAction.CallbackContext context)
		{
			if (FocusWhilePressed && m_PrevScopeWhenPressed) {

				// Cancel was called because of newly focused scope resetting all the actions.
				if (UIScope.IsCurrentlySwitchingActiveScopes(m_PlayerContext.GetRootObject())) {
					// Just cancel, the new focus is more important.
					// NOTE: resetting actions doesn't mean the user released the button. When this is a trigger, while releasing, it will fire new performed event, stealing the focus. :(
					m_PrevScopeWhenPressed = null;
					m_TargetScopeWhenPressed = null;
					return;
				}

				UIScope nextScope = m_PrevScopeWhenPressed;

				var visitedScopes = new HashSet<UIScope>() { nextScope };

				while (nextScope != null && !nextScope.isActiveAndEnabled) {
					nextScope = visitedScopes.Contains(nextScope.LastFocusedScope)
							? null
							: nextScope.LastFocusedScope
						;
				}

				m_PrevScopeWhenPressed = null;
				m_TargetScopeWhenPressed = null;

				if (nextScope) {
					nextScope.Focus();
				}
			}
		}

		private void OnScopeFocused(UIScope scope)
		{
			// Just cancel, the new focus is more important.
			if (m_PrevScopeWhenPressed && m_PlayerContext.GetRootObject() == scope.PlayerContext && scope != m_TargetScopeWhenPressed) {
				m_PrevScopeWhenPressed = null;
				m_TargetScopeWhenPressed = null;
			}
		}

		protected UIScope GetTargetScopeToFocus()
		{
			UIScope nextScope = null;

			//
			// NOTE: Most of this code is copy-pasted from the UIScope.OnDisable().
			//
			switch (FocusPolicy) {

				case FocusPolicyType.FocusScope:
					if (FocusScopes.Count == 0) {
						nextScope = GetComponentInParent<UIScope>();
						if (nextScope) {
							FocusScopes.Add(nextScope);
						}
					}

					nextScope = FocusScopes.FirstOrDefault();
					break;

				case FocusPolicyType.FocusScopeWithHighestDepth:
					IReadOnlyCollection<UIScope> registeredScopes = UIScope.GetRegisteredScopes(m_PlayerContext.GetRootObject());
					if (registeredScopes == null)
						return null;

					nextScope = registeredScopes.FirstOrDefault();
					foreach(UIScope scope in registeredScopes) {
						if (nextScope.ScopeDepth < scope.ScopeDepth && scope.isActiveAndEnabled) {
							nextScope = scope;
						}
					}
					break;

				case FocusPolicyType.FocusPreviousScope:
					nextScope = UIScope.FocusedScope(m_PlayerContext.GetRootObject())?.LastFocusedScope;

					if (nextScope == null)
						break;

					var visitedScopes = new HashSet<UIScope>() { nextScope };

					while(nextScope != null && !nextScope.isActiveAndEnabled) {
						nextScope = visitedScopes.Contains(nextScope.LastFocusedScope)
								? null
								: nextScope.LastFocusedScope
							;
					}
					break;

				case FocusPolicyType.FocusParentScope:
					Transform scopeTransform = transform.parent;
					while(scopeTransform) {
						UIScope scope = scopeTransform.GetComponent<UIScope>();
						if (scope && scope.isActiveAndEnabled) {
							nextScope = scope;
							break;
						}

						scopeTransform = scopeTransform.parent;
					}
					break;

				case FocusPolicyType.FocusFirstEnabledScopeFromList:
					nextScope = FocusScopes.FirstOrDefault(s => s.isActiveAndEnabled);
					break;

				default:
					throw new NotSupportedException(FocusPolicy.ToString());
			}

			return nextScope;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HotkeyFocusScopeElement), true)]
	[CanEditMultipleObjects]
	internal class HotkeyFocusScopeElementEditor : HotkeyBaseScopeElementEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			var focusPolicyProperty = serializedObject.FindProperty(nameof(HotkeyFocusScopeElement.FocusPolicy));
			var scopesProperty = serializedObject.FindProperty(nameof(HotkeyFocusScopeElement.FocusScopes));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyFocusScopeElement.FocusWhilePressed)));
			EditorGUILayout.PropertyField(focusPolicyProperty);

			var focusPolicy = (HotkeyFocusScopeElement.FocusPolicyType) focusPolicyProperty.intValue;

			switch (focusPolicy) {
				case HotkeyFocusScopeElement.FocusPolicyType.FocusScope:
					UIScope scope = scopesProperty.arraySize != 0 ? (UIScope) scopesProperty.GetArrayElementAtIndex(0).objectReferenceValue : null;

					UIScope nextScope = (UIScope) EditorGUILayout.ObjectField("Focused Scope", scope, typeof(UIScope), true);
					if (nextScope != scope) {
						if (scopesProperty.arraySize == 0) {
							scopesProperty.InsertArrayElementAtIndex(0);
						}

						scopesProperty.GetArrayElementAtIndex(0).objectReferenceValue = nextScope;
					}
					break;

				case HotkeyFocusScopeElement.FocusPolicyType.FocusScopeWithHighestDepth:
				case HotkeyFocusScopeElement.FocusPolicyType.FocusPreviousScope:
				case HotkeyFocusScopeElement.FocusPolicyType.FocusParentScope:
					break;

				case HotkeyFocusScopeElement.FocusPolicyType.FocusFirstEnabledScopeFromList:
					EditorGUILayout.PropertyField(scopesProperty);
					break;

				default:
					throw new NotSupportedException(focusPolicy.ToString());
			}

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif
}

#endif