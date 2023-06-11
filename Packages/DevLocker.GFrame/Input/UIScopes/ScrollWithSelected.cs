using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Forces scroll view to follow selection.
	/// </summary>
	public class ScrollWithSelected : MonoBehaviour
	{
		public ScrollRect ScrollRect;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		private GameObject m_LastSelectedObject;

		void Awake()
		{
			if (ScrollRect == null) {
				ScrollRect = GetComponent<ScrollRect>();
			}

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		void LateUpdate()
		{
			if (!m_PlayerContext.IsActive)
				return;

			if (m_LastSelectedObject != m_PlayerContext.SelectedGameObject) {
				m_LastSelectedObject = m_PlayerContext.SelectedGameObject;

				if (m_LastSelectedObject && m_LastSelectedObject.transform.IsChildOf(ScrollRect.transform)) {

					Vector2 contentPos = GetScrollSnapToPosition(ScrollRect, m_LastSelectedObject.GetComponent<RectTransform>());
					ScrollRect.content.localPosition = contentPos;
				}
			}
		}

		/// <summary>
		/// Calculate scroll position so child is displayed.
		/// Copied from https://stackoverflow.com/a/50191835 with some fixes.
		/// </summary>
		public static Vector2 GetScrollSnapToPosition(ScrollRect scrollRect, RectTransform child)
		{
			Canvas.ForceUpdateCanvases();

			Vector2 viewportLocalPosition = scrollRect.viewport.localPosition;
			Vector2 childLocalPosition = child.localPosition;

			float horizontalMax = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
			float verticalMax = scrollRect.content.rect.height - scrollRect.viewport.rect.height;

			Vector2 result = Vector2.zero;

			if (scrollRect.horizontal) {
				result.x = Mathf.Clamp(0 - (viewportLocalPosition.x + childLocalPosition.x), -horizontalMax, 0f);
			}
			if (scrollRect.vertical) {
				result.y = Mathf.Clamp(0 - (viewportLocalPosition.y + childLocalPosition.y), 0f, verticalMax);
			}

			return result;
		}
	}
}