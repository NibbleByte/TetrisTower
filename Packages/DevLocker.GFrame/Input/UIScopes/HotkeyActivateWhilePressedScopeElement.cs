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
	/// Activates target game object on specified InputAction. When released, deactivates the target objects.
	/// Note that this component will enable the input action and it needs to stay enabled to be invoked.
	/// </summary>
	public class HotkeyActivateWhilePressedScopeElement : HotkeyBaseScopeElement
	{
		[HideInInspector]
		public List<GameObject> TargetObjects = new List<GameObject>();

		protected override void OnEnable()
		{
			base.OnEnable();

			foreach(GameObject target in TargetObjects) {
				if (target) {
					target.SetActive(false);
				}
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			foreach (GameObject target in TargetObjects) {
				if (target) {
					target.SetActive(false);
				}
			}

		}

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			foreach (GameObject target in TargetObjects) {
				if (target) {
					target.SetActive(true);
				}
			}
		}

		protected override void OnCancel(InputAction.CallbackContext context)
		{
			foreach (GameObject target in TargetObjects) {
				if (target) {
					target.SetActive(false);
				}
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HotkeyActivateWhilePressedScopeElement), true)]
	[CanEditMultipleObjects]
	internal class HotkeyActivateWhilePressedScopeElementEditor : HotkeyBaseScopeElementEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyActivateWhilePressedScopeElement.TargetObjects)));

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif
}

#endif