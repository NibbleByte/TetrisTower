#if USE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Adds options to select <see cref="InputActionMap"/> from specified input asset.
	/// </summary>
	[Serializable]
	public struct InputActionMapPicker
	{
		[SerializeField]
		private InputActionAsset m_Asset;

		[SerializeField]
		private string m_InputActionMapId;

		public InputActionAsset Asset => m_Asset;

		public InputActionMap Map => m_Asset.FindActionMap(m_InputActionMapId, throwIfNotFound: true);

		public bool HasBrokenReference => m_Asset && !string.IsNullOrEmpty(m_InputActionMapId) && m_Asset.FindActionMap(m_InputActionMapId) == null;

		public bool IsValid => m_Asset && m_Asset.FindActionMap(m_InputActionMapId) != null;

		public void SetMap(InputActionAsset asset, string mapId)
		{
			m_Asset = asset;
			m_InputActionMapId = mapId;
		}

		public override string ToString()
		{
			return Asset ? $"{Asset.name}.{m_InputActionMapId}" : "Empty Map";
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputActionMapPicker))]
	internal class InputActionMapPickerPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			SerializedProperty assetProperty = property.FindPropertyRelative("m_Asset");
			SerializedProperty mapIdProperty = property.FindPropertyRelative("m_InputActionMapId");

			if (assetProperty.objectReferenceValue == null) {
				assetProperty.objectReferenceValue = EditorGUI.ObjectField(position, assetProperty.objectReferenceValue, typeof(InputActionAsset), false);
			} else {
				position.width /= 2f;
				assetProperty.objectReferenceValue = EditorGUI.ObjectField(position, assetProperty.objectReferenceValue, typeof(InputActionAsset), false);
				if (assetProperty.objectReferenceValue != null) {

					position.x += position.width;

					var asset = (InputActionAsset)assetProperty.objectReferenceValue;
					var mapId = mapIdProperty.stringValue;

					Guid mapGuid;
					if (string.IsNullOrEmpty(mapId)) {
						mapGuid = new Guid();
					} else {
						try {
							mapGuid = new Guid(mapId);	// If guid format is invalid, exception is thrown.
						} catch (Exception) {
							mapGuid = new Guid();
						}
					}

					var maps = asset.actionMaps.ToList();
					int selectedIndex = maps.FindIndex(m => m.id == mapGuid);

					EditorGUI.BeginChangeCheck();
					selectedIndex = EditorGUI.Popup(position, selectedIndex, maps.Select(m => m.name).ToArray());
					if (EditorGUI.EndChangeCheck()) {
						if (selectedIndex == -1) {
							mapIdProperty.stringValue = "";
						} else {
							mapIdProperty.stringValue = maps[selectedIndex].id.ToString();
						}
					}
				}

			}

			EditorGUI.EndProperty();
		}
	}
#endif

}
#endif