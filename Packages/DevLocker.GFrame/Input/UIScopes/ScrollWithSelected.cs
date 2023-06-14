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

		[Tooltip("Margin when snapping")]
		public Vector2 Margin;

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
					Vector2 contentPos = GetScrollSnapToPosition(ScrollRect, m_LastSelectedObject.GetComponent<RectTransform>(), Margin);
					ScrollRect.content.localPosition = contentPos;
				}
			}
		}

		/// <summary>
		/// Calculate scroll position so child is displayed.
		/// Copied from https://stackoverflow.com/a/30769550/4612666 with some fixes.
		/// </summary>
		public static Vector2 GetScrollSnapToPosition(ScrollRect scrollRect, RectTransform child, Vector2 margin)
		{
			Canvas.ForceUpdateCanvases();

			Vector2 contentPos = scrollRect.transform.InverseTransformPoint(scrollRect.content.position);
			Vector2 childPosLocal = new Vector2(child.rect.xMin - margin.x, child.rect.yMax + margin.y);
			Vector2 childPos = scrollRect.transform.InverseTransformPoint(child.TransformPoint(childPosLocal));

			Vector2 endPos = contentPos - childPos;

			float horizontalMax = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
			float verticalMax = scrollRect.content.rect.height - scrollRect.viewport.rect.height;

			Vector2 result = Vector2.zero;

			if (scrollRect.horizontal && horizontalMax > 0f) {
				result.x = Mathf.Clamp(endPos.x, -horizontalMax, 0f);
			}
			if (scrollRect.vertical && verticalMax > 0f) {
				result.y = Mathf.Clamp(endPos.y, 0f, verticalMax);
			}

			return result;
		}
	}
}