using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TetrisTower.Core.UI
{
	public static class UIUtils
	{
		private static List<RaycastResult> m_RayCastResultsCache = new List<RaycastResult>();

		public static IReadOnlyList<RaycastResult> RaycastUIElements(Vector3 position)
		{
			PointerEventData eventData = new PointerEventData(EventSystem.current);
			eventData.position = position;
			m_RayCastResultsCache.Clear();
			EventSystem.current.RaycastAll(eventData, m_RayCastResultsCache);
			return m_RayCastResultsCache;
		}
	}
}