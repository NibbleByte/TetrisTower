#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Put next to or under a <see cref="UnityEngine.UI.ScrollRect"/> to start scrolling when the InputAction gets invoked.
	/// Input action should be:
	///		> Action Type: Value
	///		> Control Type: Axis
	///		> Bindings: 1D Axis [-1, 1]
	/// </summary>
	public class HotkeyScrollScopeElement : HotkeyBaseScopeElement
	{
		public enum DirectionType
		{
			Horizontal,
			Vertical,
		}

		public ScrollRect ScrollRect;

		public DirectionType Direction = DirectionType.Vertical;
		public float ScrollSpeed = 100f;
		public bool Invert = false;

		private float m_ScrollValue = 0f;


		protected override void OnDisable()
		{
			base.OnDisable();

			m_ScrollValue = 0f;
		}

		protected override void OnInvoke(InputAction.CallbackContext context)
		{
			m_ScrollValue = context.ReadValue<float>();
		}

		protected override void OnCancel(InputAction.CallbackContext context)
		{
			m_ScrollValue = 0f;
		}

		void Update()
		{
			if (ScrollRect == null) {
				ScrollRect = GetComponentInParent<ScrollRect>();
			}

			if (ScrollRect) {

				Vector3 dir = Direction == DirectionType.Horizontal ? Vector3.left : Vector3.down;
				if (Invert) {
					dir *= -1;
				}

				ScrollRect.content.position += dir * ScrollSpeed * m_ScrollValue * Time.deltaTime;
			}
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			// OnValidate() gets called even if object is not active.
			var scrollRect = GetComponentInParent<ScrollRect>(true);
			if (scrollRect == null) {
				Debug.LogError($"[Input] No valid ScrollRect was found for HotkeyButton {name}", this);
				return;
			}
		}
	}
}

#endif