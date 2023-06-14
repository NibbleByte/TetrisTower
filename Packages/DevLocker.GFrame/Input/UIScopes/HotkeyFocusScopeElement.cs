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
	/// Note that this action has to be enabled in order to be invoked.
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
		public FocusPolicyType FocusPolicy;

		[HideInInspector]
		public List<UIScope> FocusScopes = new List<UIScope>();

		protected override void OnInvoke(InputAction.CallbackContext context)
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
						return;

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

			if (nextScope) {
				if (nextScope.IsFocused || !nextScope.isActiveAndEnabled)
					return;

				nextScope.Focus();
			} else {
				Debug.LogWarning($"[Input] No scope to focus for {name}", this);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HotkeyFocusScopeElement), true)]
	[CanEditMultipleObjects]
	internal class HotkeyFocusScopeElementEditor : HotkeyBaseScopeElementEditor
	{
		private static HotkeyFocusScopeElement.FocusPolicyType[] s_HotkeyFocusScopeElementFocusPolicyValues = (HotkeyFocusScopeElement.FocusPolicyType[]) Enum.GetValues(typeof(HotkeyFocusScopeElement.FocusPolicyType));

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			var focusPolicyProperty = serializedObject.FindProperty(nameof(HotkeyFocusScopeElement.FocusPolicy));
			var scopesProperty = serializedObject.FindProperty(nameof(HotkeyFocusScopeElement.FocusScopes));
			EditorGUILayout.PropertyField(focusPolicyProperty);

			var focusPolicy = s_HotkeyFocusScopeElementFocusPolicyValues[focusPolicyProperty.enumValueIndex];

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