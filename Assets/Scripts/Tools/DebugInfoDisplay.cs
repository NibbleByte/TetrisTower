using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Tools
{
	public class DebugInfoDisplay : MonoBehaviour
	{
		public TextMeshProUGUI DisplayText;

		public bool DisplayScreenInfo = true;

		public bool DisplayPointerInfo = true;

		private bool m_PointerPressed;
		private Vector2 m_StartPointerPos;
		private float m_StartPointerTime;
		private Vector2 m_LastPointerPos;
		private float m_LastPointerPressDuration;
		private Vector2 m_LastFullPointerPressDistance;
		private Vector2 m_MaxPointerPressDistancePerFrame;

		void Awake()
		{
			if (DisplayText == null) {
				DisplayText = GetComponent<TextMeshProUGUI>();
			}
		}

		void Update()
		{
			var text = new StringBuilder();

			if (DisplayScreenInfo) {
				text.AppendLine($"Screen: {Screen.width} x {Screen.height} ({Screen.dpi} dpi)");
			}

			if (DisplayPointerInfo && Touchscreen.current != null) {

				if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled) {
					UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
				}

				if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count > 0) {

					var pointerPos = Pointer.current.position.ReadValue();

					if (!m_PointerPressed) {
						m_PointerPressed = true;
						m_StartPointerPos = pointerPos;
						m_StartPointerTime = Time.time;
						m_LastPointerPos = pointerPos;
						m_MaxPointerPressDistancePerFrame = Vector2.zero;
					}

					m_LastFullPointerPressDistance = pointerPos - m_StartPointerPos;
					m_LastPointerPressDuration = Time.time - m_StartPointerTime;
					if (m_MaxPointerPressDistancePerFrame.magnitude < (pointerPos - m_LastPointerPos).magnitude) {
						m_MaxPointerPressDistancePerFrame = pointerPos - m_LastPointerPos;
					}

					m_LastPointerPos = pointerPos;

					text.AppendLine($"Pointer: {pointerPos}");
				} else {
					m_PointerPressed = false;
					text.AppendLine($"Pointer: none");
				}

				text.AppendLine($"Drag: {m_LastFullPointerPressDistance} {m_LastFullPointerPressDistance.magnitude:0.##}");
				text.AppendLine($"Drag Max: {m_MaxPointerPressDistancePerFrame} {m_MaxPointerPressDistancePerFrame.magnitude:0.##}");
				text.AppendLine($"Drag Time: {m_LastPointerPressDuration:0.####}");
			}

			DisplayText.text = text.ToString();
		}
	}
}