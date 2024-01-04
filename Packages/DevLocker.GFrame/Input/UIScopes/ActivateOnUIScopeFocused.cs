#if USE_INPUT_SYSTEM

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Monitors the <see cref="UIScope"/> and shows or hides objects if focused.
	/// </summary>
	public class ActivateOnUIScopeFocused : MonoBehaviour
	{
		[Tooltip("Check to work with multiple objects (advanced interface)")]
		public bool MultipleObjects = false;

		[Tooltip("Activate when scope(s) are NOT focused.")]
		public bool Invert = false;

		[Tooltip("Hide activated objects if scope is inactive in the hierarchy.")]
		public bool HideObjectsOnInactiveScope = true;

		[Tooltip("When this UIScope is focused, activate the target object.")]
		public UIScope OnScopeFocused;
		[Tooltip("Object to be activated.")]
		public GameObject ActivatedObject;

		[Tooltip("When any of these scopes are focused, activate the target objects.")]
		public List<UIScope> OnScopesFocused;
		[Tooltip("Objects to be activated.")]
		public List<GameObject> ActivatedObjects;

		private bool IsScopeFocused() => MultipleObjects
			? OnScopesFocused.Any(s => s && s.IsFocused)
			: OnScopeFocused && OnScopeFocused.IsFocused;

		private bool IsScopeActiveAndEnabled() => MultipleObjects
			? OnScopesFocused.Any(s => s && s.isActiveAndEnabled)
			: OnScopeFocused && OnScopeFocused.isActiveAndEnabled;

		void Reset()
		{
			OnScopeFocused = GetComponentInParent<UIScope>(true);
		}

		void Awake()
		{
			if (!MultipleObjects && OnScopeFocused == null) {
				OnScopeFocused = GetComponent<UIScope>();
			}
		}

		void OnDisable()
		{
			if (MultipleObjects) {
				foreach (GameObject obj in ActivatedObjects) {
					if (obj) {
						obj.SetActive(false);
					}
				}
			} else {
				if (ActivatedObject) {
					ActivatedObject.SetActive(false);
				}
			}
		}

		public void Update()
		{
			bool focused = IsScopeFocused();
			bool activate = Invert ? !focused : focused;

			if (!focused && HideObjectsOnInactiveScope) {
				activate = IsScopeActiveAndEnabled() ? activate : false;
			}


			if (MultipleObjects) {
				foreach (GameObject obj in ActivatedObjects) {
					if (obj.activeSelf != activate) {
						obj.SetActive(activate);
					}
				}

			} else {
				if (ActivatedObject.activeSelf != activate) {
					ActivatedObject.SetActive(activate);
				}
			}
		}

		void OnValidate()
		{
			if (MultipleObjects) {
				if (OnScopesFocused != null && (OnScopesFocused.Count == 0 || OnScopesFocused.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnScopesFocused)}...", this);
				} else if (ActivatedObjects != null && (ActivatedObjects.Count == 0 || ActivatedObjects.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObjects)}...", this);
				}

			} else {

#if UNITY_EDITOR
				if (OnScopeFocused == null && ActivatedObject == null && !Application.isPlaying) {
					OnScopeFocused = GetComponent<UIScope>();

					UnityEditor.EditorUtility.SetDirty(this);
				}
#endif

				if (OnScopeFocused == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnScopeFocused)}...", this);
				} else if (ActivatedObject == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObject)}...", this);
				}

			}
		}
	}


#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(ActivateOnUIScopeFocused), true)]
	[UnityEditor.CanEditMultipleObjects]
	internal class ActivateOnFocusedUIScopeEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			var multipleObjectsProp = serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.MultipleObjects));
			UnityEditor.EditorGUILayout.PropertyField(multipleObjectsProp);

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.HideObjectsOnInactiveScope)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.Invert)));

			if (multipleObjectsProp.boolValue) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.OnScopesFocused)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.ActivatedObjects)));

			} else {

				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.OnScopeFocused)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnUIScopeFocused.ActivatedObject)));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}

#endif