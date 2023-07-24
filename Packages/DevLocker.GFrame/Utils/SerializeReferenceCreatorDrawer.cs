#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;


namespace DevLocker.GFrame.Utils
{
#if UNITY_EDITOR
	/// <summary>
	/// Use this class with [SerializeReference] attribute to have a "Create" button next to such fields.
	/// By default [SerializeReference] fields display empty data in the inspector - there is no UI to create data instance.
	/// Pressing the "Create" button will ask you to select the type to be instantiated - any class that inherits or implements the target field type.
	///
	/// Create custom drawer of your class that inherits from this one by specifying the target class type itself as <typeparamref name="T"/>.
	/// It works properly if [SerializeReference] is not present.
	///
	/// If you want to have custom drawing of the data, override the <see cref="GetPropertyHeight_Custom(SerializedProperty, GUIContent)"/> and
	/// <see cref="OnGUI_Custom(Rect, SerializedProperty, GUIContent, bool)"/>.
	///
	/// Example:
	/// [CustomPropertyDrawer(typeof(MyClass))]
	/// public class MyClassDrawer : SerializeReferenceCreatorDrawer<MyClass>
	/// {
	/// }
	///
	/// </summary>
	/// <typeparam name="T">Class type to be drawn.</typeparam>
	public class SerializeReferenceCreatorDrawer<T> : PropertyDrawer
	{
		protected const float ReferenceButtonWidth = 60f;

		protected GUIStyle m_TypeLabelStyle;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.ManagedReference && string.IsNullOrEmpty(property.managedReferenceFullTypename)) {
				return EditorGUIUtility.singleLineHeight;
			} else {
				return GetPropertyHeight_Custom(property, label);
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (m_TypeLabelStyle == null) {
				m_TypeLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
				m_TypeLabelStyle.wordWrap = false;
				m_TypeLabelStyle.alignment = TextAnchor.MiddleRight;
			}

			// This property doesn't have the [SerializeReference] attribute.
			if (property.propertyType != SerializedPropertyType.ManagedReference) {
				OnGUI_Custom(position, property, label, false);
				return;
			}

			var buttonRect = position;
			buttonRect.x += buttonRect.width - ReferenceButtonWidth;
			buttonRect.width = ReferenceButtonWidth;
			buttonRect.height = EditorGUIUtility.singleLineHeight;

			bool isReferenceEmpty = string.IsNullOrEmpty(property.managedReferenceFullTypename);

			if (isReferenceEmpty) {
				label = EditorGUI.BeginProperty(position, label, property);

				EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

				EditorGUI.EndProperty();

				if (QuickButton(buttonRect, Color.green, "Create")) {

					List<Type> availableTypes = AppDomain.CurrentDomain.GetAssemblies()
						.SelectMany(assembly => assembly.GetTypes())
						.Where(type => type.IsClass && !type.IsAbstract && !type.IsValueType)
						//.Where(type => type.GetCustomAttribute<SerializableAttribute>(true) != null)	// TODO: Doesn't find parent attributes.
						.Where(type => typeof(T).IsAssignableFrom(type))
						.Where(type => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null)
						.ToList()
						;

					availableTypes.Sort((a, b) => a.Name.CompareTo(b.Name));

					var menu = new GenericMenu();
					foreach(Type type in availableTypes) {
						menu.AddItem(new GUIContent(type.Name), false, OnTypeSelected, new KeyValuePair<SerializedProperty, Type>(property, type));
					}

					menu.ShowAsContext();
				}

			} else {
				OnGUI_Custom(position, property, label, true);
			}
		}

		private void OnTypeSelected(object obj)
		{
			var pair = (KeyValuePair<SerializedProperty, Type>)obj;

			pair.Key.managedReferenceValue = Activator.CreateInstance(pair.Value);
			pair.Key.serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// Override this to change the height of the displayed data.
		/// </summary>
		protected virtual float GetPropertyHeight_Custom(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property);
		}

		/// <summary>
		/// Override to customize how data is displayed.
		/// NOTE: To have a clear button, make sure you call <see cref="DrawClearButton(SerializedProperty, Rect, Color, string)"/>!
		/// </summary>
		protected virtual void OnGUI_Custom(Rect position, SerializedProperty property, GUIContent label, bool isManagedReference)
		{
			if (isManagedReference) {

				var buttonRect = position;
				buttonRect.x += buttonRect.width - ReferenceButtonWidth;
				buttonRect.width = ReferenceButtonWidth;
				buttonRect.height = EditorGUIUtility.singleLineHeight;

				var typeLabelRect = position;
				typeLabelRect.width -= ReferenceButtonWidth;
				typeLabelRect.height = EditorGUIUtility.singleLineHeight;

				DrawManagedTypeLabel(typeLabelRect, property, Color.white * 0.8f);

				DrawClearButton(buttonRect, property, Color.red, "Clear");
			}

			label = EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			EditorGUI.PropertyField(position, property, true);

			EditorGUI.EndProperty();
		}

		/// <summary>
		/// Draw label with the name of the managed type.
		/// If type name contains underscores "_", it will be truncated to the first underscore.
		/// </summary>
		protected void DrawManagedTypeLabel(Rect typeLabelRect, SerializedProperty property, Color color, GUIStyle style = null)
		{
			if (style == null) {
				style = m_TypeLabelStyle;
			}

			// If missing, start index is 0, so it's ok.
			int typeIndex = property.managedReferenceFullTypename.LastIndexOf(".") + 1;

			// End should be the first underscore or string length.
			int typeEndIndex = property.managedReferenceFullTypename.IndexOf("_", typeIndex + 1) - 1;
			if (typeEndIndex < 0) {
				typeEndIndex = property.managedReferenceFullTypename.Length - 1;
			}

			int assemblyIndex = property.managedReferenceFullTypename.IndexOf(" ") + 1;	// Again, missing will produce 0.

			GUIContent typeName = new GUIContent(property.managedReferenceFullTypename.Substring(typeIndex, typeEndIndex - typeIndex + 1));
			typeName.tooltip = $"Full managed type name:\n{property.managedReferenceFullTypename.Substring(assemblyIndex)}";

			Vector2 labelSize = style.CalcSize(typeName);
			typeLabelRect.x += typeLabelRect.width - labelSize.x;
			typeLabelRect.width = labelSize.x;

			var prevColor = GUI.color;
			GUI.color = color;


			if (GUI.Button(typeLabelRect, typeName, style)) {
				MonoScript asset = AssetDatabase.FindAssets($"t:script {property.managedReferenceFullTypename.Substring(typeIndex)}")
							.Select(AssetDatabase.GUIDToAssetPath)
							.Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
							.FirstOrDefault();

				if (asset) {
					AssetDatabase.OpenAsset(asset);
					GUIUtility.ExitGUI();
				}
			}
			EditorGUIUtility.AddCursorRect(typeLabelRect, MouseCursor.Link);

			GUI.color = prevColor;
		}

		/// <summary>
		/// Draw the "Clear" button the way you want.
		/// NOTE: if button is pressed, <see cref="GUIUtility.ExitGUI"/> is called, preventing the code from resuming with empty reference.
		/// </summary>
		protected void DrawClearButton(Rect buttonRect, SerializedProperty property, Color color, string text)
		{
			if (QuickButton(buttonRect, color, text)) {
				property.managedReferenceValue = null;
				property.serializedObject.ApplyModifiedProperties();
				GUIUtility.ExitGUI();
			}
		}

		private bool QuickButton(Rect buttonRect, Color color, string text)
		{
			var prevBackground = GUI.backgroundColor;
			bool clicked = false;

			GUI.backgroundColor = color;
			if (GUI.Button(buttonRect, text)) {
				clicked = true;
			}

			GUI.backgroundColor = prevBackground;

			return clicked;
		}
	}
#endif
}
