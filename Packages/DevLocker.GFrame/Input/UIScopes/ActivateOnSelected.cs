using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When the <see cref="Selectable"/>s are selected, activate the target game object.
	/// Use this to display or hide hotkeys next to buttons when selected.
	/// </summary>
	public class ActivateOnSelected : MonoBehaviour
	{
		[Tooltip("Check to work with multiple objects (advanced interface)")]
		public bool MultipleObjects = false;

		[Tooltip("When this object is selected, activate the other.")]
		public Selectable OnSelectedObject;
		[Tooltip("Object to be activated.")]
		public GameObject ActivatedObject;

		[Tooltip("When any of these objects is selected, activate the others.")]
		public Selectable[] OnSelectedObjects;
		[Tooltip("Objects to be activated.")]
		public GameObject[] ActivatedObjects;

		private GameObject m_LastSelectedObject;

		private void Awake()
		{
			if (!MultipleObjects && OnSelectedObject == null) {
				OnSelectedObject = GetComponent<Selectable>();
			}
		}

		void Update()
		{
			if (EventSystem.current == null)
				return;

			if (m_LastSelectedObject != EventSystem.current.currentSelectedGameObject) {
				m_LastSelectedObject = EventSystem.current.currentSelectedGameObject;

				if (MultipleObjects) {
					bool activate;
					if (m_LastSelectedObject && OnSelectedObjects.Any(s => m_LastSelectedObject.transform.IsChildOf(s.transform))) {
						activate = true;
					} else {
						activate = false;
					}

					foreach(GameObject obj in ActivatedObjects) {
						obj.SetActive(activate);
					}

				} else {

					if (m_LastSelectedObject && m_LastSelectedObject.transform.IsChildOf(OnSelectedObject.transform)) {
						ActivatedObject.SetActive(true);
					} else {
						ActivatedObject.SetActive(false);
					}
				}
			}
		}

		void OnValidate()
		{
			if (MultipleObjects) {
				if (OnSelectedObjects != null && (OnSelectedObjects.Length == 0 || OnSelectedObjects.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnSelectedObjects)}...", this);
				} else if (ActivatedObjects != null && (ActivatedObjects.Length == 0 || ActivatedObjects.Any(o => o == null))) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObjects)}...", this);
				}

			} else {

#if UNITY_EDITOR
				if (OnSelectedObject == null && ActivatedObject == null && !Application.isPlaying) {
					OnSelectedObject = GetComponent<Selectable>();

					UnityEditor.EditorUtility.SetDirty(this);
				}
#endif

				if (OnSelectedObject == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(OnSelectedObject)}...", this);
				} else if (ActivatedObject == null) {
					Debug.LogError($"\"{name}\" has missing {nameof(ActivatedObject)}...", this);
				}

			}
		}
	}


#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(ActivateOnSelected), true)]
	[UnityEditor.CanEditMultipleObjects]
	internal class ActivateOnSelectedEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			var multipleObjectsProp = serializedObject.FindProperty(nameof(ActivateOnSelected.MultipleObjects));
			UnityEditor.EditorGUILayout.PropertyField(multipleObjectsProp);

			if (multipleObjectsProp.boolValue) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnSelected.OnSelectedObjects)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnSelected.ActivatedObjects)));

				if (GUILayout.Button("Add all child Selectables")) {
					var selectablesProp = serializedObject.FindProperty(nameof(ActivateOnSelected.OnSelectedObjects));
					Selectable[] selectables = ((ActivateOnSelected)target).transform.GetComponentsInChildren<Selectable>();
					foreach(Selectable selectable in selectables) {
						selectablesProp.InsertArrayElementAtIndex(selectablesProp.arraySize);
						selectablesProp.GetArrayElementAtIndex(selectablesProp.arraySize - 1).objectReferenceValue = selectable;
					}
				}

			} else {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnSelected.OnSelectedObject)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ActivateOnSelected.ActivatedObject)));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif

}