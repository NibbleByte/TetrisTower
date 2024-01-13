#if USE_INPUT_SYSTEM

using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Calls UnityEvent on specified InputAction.
	/// Note that this component will enable the input action and it needs to stay enabled to be invoked.
	/// </summary>
	public class HotkeyEventScopeElement : HotkeyBaseScopeElement
	{
		// Base layout names can be found in InputManager.InitializeData()
		internal const string AxisLayoutName = "Axis";
		internal const string Vector2LayoutName = "Vector2";

		[UnityEngine.Serialization.FormerlySerializedAs("OnAction")]
		public UnityEvent OnPerformed;

		public UnityEvent OnStarted;

		public UnityEvent OnCancelled;

		private const string DynamicEventsTooltip = "Will be called only once unless direction changes or action is cancelled.";

		public UnityEvent<float> OnAxisPerformed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnAxisNegativePerformed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnAxisPositivePerformed;

		public UnityEvent<Vector2> OnVector2Performed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnVector2UpPerformed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnVector2DownPerformed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnVector2LeftPerformed;
		[Tooltip(DynamicEventsTooltip)] public UnityEvent OnVector2RightPerformed;

		private float m_LastAxisSigned = 0f;
		private Vector2 m_LastVectorSigned = Vector2.zero;

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			switch (context.action.expectedControlType) {

				case AxisLayoutName:
					float axisValue = context.action.ReadValue<float>();
					OnAxisPerformed.Invoke(axisValue);

					// Note: sign doesn't return 0 :(
					float axisSigned = Mathf.Sign(axisValue);

					if (axisSigned != m_LastAxisSigned) {
						m_LastAxisSigned = axisSigned;

						if (axisSigned > 0) {
							OnAxisPositivePerformed.Invoke();
						} else if (axisSigned < 0) {
							OnAxisNegativePerformed.Invoke();
						}
					}
					break;

				case Vector2LayoutName:
					Vector2 vectorValue = context.action.ReadValue<Vector2>();
					OnVector2Performed.Invoke(vectorValue);

					// Note: sign doesn't return 0 :(
					Vector2 vectorSigned = Mathf.Abs(vectorValue.x) > Mathf.Abs(vectorValue.y)
						? new Vector2(Mathf.Sign(vectorValue.x), 0)
						: new Vector2(0, Mathf.Sign(vectorValue.y))
						;

					if (vectorSigned != m_LastVectorSigned) {
						m_LastVectorSigned = vectorSigned;

						if (vectorSigned.x > 0) {
							OnVector2RightPerformed.Invoke();
						} else if (vectorSigned.x < 0) {
							OnVector2LeftPerformed.Invoke();
						} else if (vectorSigned.y > 0) {
							OnVector2UpPerformed.Invoke();
						} else if (vectorSigned.y < 0) {
							OnVector2DownPerformed.Invoke();
						}
					}
					break;

				default:
					OnPerformed.Invoke();

					break;
			}
		}

		protected override void OnStart(InputAction.CallbackContext context)
		{
			OnStarted.Invoke();
		}

		protected override void OnCancel(InputAction.CallbackContext context)
		{
			OnCancelled.Invoke();

			m_LastAxisSigned = 0;
			m_LastVectorSigned = Vector2.zero;
		}
	}

	[CustomEditor(typeof(HotkeyEventScopeElement), true)]
	[CanEditMultipleObjects]
	internal class HotkeyEventScopeElementEditor : HotkeyBaseScopeElementEditor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawScriptProperty();

			DrawHotkeyBaseProperties();

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();

			bool isAxisAction = serializedObject.targetObjects
				.OfType<HotkeyBaseScopeElement>()
				.Any(hotkey => hotkey.InputAction?.action?.expectedControlType == HotkeyEventScopeElement.AxisLayoutName);

			bool isVectorAction = serializedObject.targetObjects
				.OfType<HotkeyBaseScopeElement>()
				.Any(hotkey => hotkey.InputAction?.action?.expectedControlType == HotkeyEventScopeElement.Vector2LayoutName);

			HotkeyEventScopeElement singleInstance = targets.Length == 1 ? (HotkeyEventScopeElement)target : null;

			if (!isAxisAction && !isVectorAction) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnPerformed)));
			}
			if (isAxisAction) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnAxisPerformed)));
			}
			if (isVectorAction) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnVector2Performed)));
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnStarted)));
			EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnCancelled)));

			if (isAxisAction) {
				EditorGUILayout.LabelField("Axis Events", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnAxisNegativePerformed)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnAxisPositivePerformed)));
			}
			if (isVectorAction) {
				EditorGUILayout.LabelField("Vector Events", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnVector2UpPerformed)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnVector2DownPerformed)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnVector2LeftPerformed)));
				EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HotkeyEventScopeElement.OnVector2RightPerformed)));
			}

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}

#endif