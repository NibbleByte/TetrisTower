#if USE_INPUT_SYSTEM
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Definition for set of InputActions grouped by some criteria (similar to the <see cref="InputActionMap"/> but can be referenced).
	/// Can be used to enable/disable them all together.
	/// If you specify input asset & map, the set will dynamically be populated with those actions instead.
	/// </summary>
	[CreateAssetMenu(fileName = "InputActionsSetDef", menuName = "GFrame/Input Actions Set Def", order = 1015)]
	public class InputActionsSetDef : ScriptableObject
	{
		public InputActionMapPicker ActionsMap;

		[Tooltip("If action map specified, these are NOT used. They are still populated so you can see the reference in the Dependency Viewer tool.")]
		[SerializeField]
		private InputActionReference[] m_InputActions;

		private bool m_OnDemandActions => ActionsMap.IsValid;

		public IEnumerable<InputAction> GetActions(IInputContext inputContext)
		{
			if (m_OnDemandActions) {

				foreach(InputAction actionFromAsset in ActionsMap.Map) {
#if UNITY_EDITOR
					// For editor purposes.
					if (inputContext == null) {
						yield return actionFromAsset;
						continue;
					}
#endif

					InputAction action = inputContext.FindActionFor(actionFromAsset.name);

					// Context references may be different? No? Too late to check.
					if (action != null) {
						yield return action;
					}
				}

			} else {
				foreach (InputActionReference actionReference in m_InputActions) {
					if (actionReference == null)
						continue;

#if UNITY_EDITOR
					// For editor purposes.
					if (inputContext == null) {
						yield return actionReference;
						continue;
					}
#endif

					InputAction action = inputContext.FindActionFor(actionReference.name);
					if (action != null) {
						yield return action;
					}
				}
			}
		}

		private bool UpdateOnDemandReferences()
		{
			if (!m_InputActions.Where(r => r != null).Select(r => r.action).SequenceEqual(ActionsMap.Map.actions)) {
#if UNITY_EDITOR
				m_InputActions = GetAssetReferencesFromAssetDatabase(ActionsMap.Asset, ActionsMap.Map);
#else
				// Can't get them easily so do nothing.
				// References are used only for Dependency Viewer results when map is specified - they are gathered from the map itself runtime, not the list of references.
#endif
				return true;
			}

			return false;
		}


#if UNITY_EDITOR

		void OnValidate()
		{
			if (ActionsMap.HasBrokenReference) {
				Debug.LogError($"[Input] \"{name}\" set has InputActionMap \"{ActionsMap}\" that doesn't exist in the {ActionsMap.Asset.name}", this);

			} else if (m_OnDemandActions) {

				if (UpdateOnDemandReferences()) {
					EditorUtility.SetDirty(this);
				}
			}
		}


		// Cause there is now way to get the references.
		private static InputActionReference[] GetAssetReferencesFromAssetDatabase(InputActionAsset actions, InputActionMap map)
		{
			if (actions == null)
				return null;

			var path = AssetDatabase.GetAssetPath(actions);
			var assets = AssetDatabase.LoadAllAssetsAtPath(path);
			return assets
				.OfType<InputActionReference>()
				.Where(r => r.action.actionMap == map)
				.ToArray();
		}

#endif

	}


#if UNITY_EDITOR

	[CustomEditor(typeof(InputActionsSetDef))]
	[CanEditMultipleObjects]
	internal class InputActionsSetDefEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.HelpBox("Groups list of InputActions as a set to be passed around. If you specify input asset & map, the set will dynamically be populated with those actions instead.", MessageType.Info);

			EditorGUI.BeginChangeCheck();

			var actionsMapProperty = serializedObject.FindProperty(nameof(InputActionsSetDef.ActionsMap));
			EditorGUILayout.PropertyField(actionsMapProperty);

			bool onDemandActions = actionsMapProperty.FindPropertyRelative("m_Asset").objectReferenceValue != null;

			EditorGUI.BeginDisabledGroup(onDemandActions);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_InputActions"));

			EditorGUI.EndDisabledGroup();

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
			}
		}

	}
#endif
}
#endif