using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Utils
{
	/// <summary>
	/// Display enum as a bit mask drop-down menu in the editor.
	/// </summary>
	public class EnumMaskAttribute : PropertyAttribute
	{
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
	internal class EnumMaskPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.BeginChangeCheck();
			int value = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
			if (EditorGUI.EndChangeCheck()) {
				property.intValue = value;
			}

			EditorGUI.EndProperty();
		}
	}
#endif

}
