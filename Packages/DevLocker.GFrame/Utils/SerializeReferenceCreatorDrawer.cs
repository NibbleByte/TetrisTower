#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace DevLocker.GFrame.Utils
{
#if UNITY_EDITOR
	public class SerializeReferenceCreatorDrawer<T> : PropertyDrawer where T: new()
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.ManagedReference && string.IsNullOrEmpty(property.managedReferenceFullTypename)) {
				return EditorGUIUtility.singleLineHeight;
			} else {
				return EditorGUI.GetPropertyHeight(property);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// This property doesn't have the [SerializeReference] attribute.
			if (property.propertyType != SerializedPropertyType.ManagedReference) {
				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

				EditorGUI.PropertyField(position, property, true);

				EditorGUI.EndProperty();
				return;
			}

			float ClearWidth = 60f;
			var clearRect = position;
			clearRect.x += clearRect.width - ClearWidth;
			clearRect.width = ClearWidth;
			clearRect.height = EditorGUIUtility.singleLineHeight;

			var prevBackground = GUI.backgroundColor;

			if (string.IsNullOrEmpty(property.managedReferenceFullTypename)) {
				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

				GUI.backgroundColor = Color.green;
				if (GUI.Button(clearRect, "Create")) {
					property.managedReferenceValue = new T();
				}
				GUI.backgroundColor = prevBackground;

				EditorGUI.EndProperty();

			} else {

				label = EditorGUI.BeginProperty(position, label, property);
				EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

				GUI.backgroundColor = Color.red;
				if (GUI.Button(clearRect, "Clear")) {
					property.managedReferenceValue = null;
				}
				GUI.backgroundColor = prevBackground;

				EditorGUI.PropertyField(position, property, true);

				EditorGUI.EndProperty();
			}

		}
	}
#endif
}
