using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TetrisTower.Tools
{
    public class DebugInfoDisplay : MonoBehaviour
    {
        public Text DisplayText;

        public bool DisplayScreenInfo = true;

        public bool DisplayPointerInfo = true;

        private bool m_PointerPressed;
        private Vector2 m_StartPointerPos;

        void Awake()
        {
            if (DisplayText == null) {
                DisplayText = GetComponent<Text>();
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
                    }

                    text.AppendLine($"Pointer: {pointerPos};\nDrag: {(pointerPos - m_StartPointerPos)}");
                } else {
                    m_PointerPressed = false;
                    text.AppendLine($"Pointer: none");
                }
            }

            DisplayText.text = text.ToString();
        }
    }
}