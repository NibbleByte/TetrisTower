using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When the <see cref="Selectable"/>s are interactable, activate/deactivate the target game object.
	/// Use this to display or hide hotkeys next to buttons when interactable.
	/// </summary>
	public class ActivateOnInteractable : MonoBehaviour
	{
		[Tooltip("Check to work with multiple objects (advanced interface)")]
		public bool MultipleObjects = false;

		[Tooltip("Activate when selectable(s) are NOT interactable.")]
		public bool Invert = false;

		[Tooltip("When this selectable is interactable, activate the target object.")]
		public Selectable OnInteractableObject;
		[Tooltip("Object to be activated.")]
		public GameObject ActivatedObject;

		[Tooltip("When any of these selectables are interactable, activate the target objects.")]
		public List<Selectable> OnInteractableObjects;
		[Tooltip("Objects to be activated.")]
		public List<GameObject> ActivatedObjects;

		private bool m_LastInteractableState;

		private bool m_HasInitialized = false;

		private bool IsInteractable() => MultipleObjects
			? OnInteractableObjects.Any(s => s && s.IsInteractable())
			: OnInteractableObject && OnInteractableObject.IsInteractable();

		void Awake()
		{
			if (!MultipleObjects && OnInteractableObject == null) {
				OnInteractableObject = GetComponent<Selectable>();
			}
		}

		public void Update()
		{
			if (m_LastInteractableState != IsInteractable() || !m_HasInitialized) {
				m_HasInitialized = true;
				m_LastInteractableState = IsInteractable();

				if (MultipleObjects) {
					foreach (GameObject obj in ActivatedObjects) {
						obj.SetActive(Invert ? !m_LastInteractableState : m_LastInteractableState);
					}

				} else {
					ActivatedObject.SetActive(Invert ? !m_LastInteractableState : m_LastInteractableState);
				}
			}
		}

		void OnValidate()
		{
			if (MultipleObjects) {
				if (OnInteractableObjects != null && (OnInteractableObjects.Count == 0 || OnInteractableObjects.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnInteractableObjects)}...", this);
				} else if (ActivatedObjects != null && (ActivatedObjects.Count == 0 || ActivatedObjects.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObjects)}...", this);
				}

			} else {

#if UNITY_EDITOR
				if (OnInteractableObject == null && ActivatedObject == null && !Application.isPlaying) {
					OnInteractableObject = GetComponent<Selectable>();

					UnityEditor.EditorUtility.SetDirty(this);
				}
#endif

				if (OnInteractableObject == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnInteractableObject)}...", this);
				} else if (ActivatedObject == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObject)}...", this);
				}

			}
		}
	}


#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(ActivateOnInteractable), true)]
	[UnityEditor.CanEditMultipleObjects]
	internal class ActivateOnInteractableEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			var multipleObjectsProp = serializedObject.FindProperty(nameof(ActivateOnInteractable.MultipleObjects));
			UnityEditor.EditorGUILayout.PropertyField(multipleObjectsProp);

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnInteractable.Invert)));

			if (multipleObjectsProp.boolValue) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnInteractable.OnInteractableObjects)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnInteractable.ActivatedObjects)));

				if (GUILayout.Button("Add all child Selectables")) {
					var selectablesProp = serializedObject.FindProperty(nameof(ActivateOnInteractable.OnInteractableObjects));
					Selectable[] selectables = ((ActivateOnInteractable)target).transform.GetComponentsInChildren<Selectable>();
					foreach(Selectable selectable in selectables) {
						selectablesProp.InsertArrayElementAtIndex(selectablesProp.arraySize);
						selectablesProp.GetArrayElementAtIndex(selectablesProp.arraySize - 1).objectReferenceValue = selectable;
					}
				}

			} else {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnInteractable.OnInteractableObject)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnInteractable.ActivatedObject)));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif

}