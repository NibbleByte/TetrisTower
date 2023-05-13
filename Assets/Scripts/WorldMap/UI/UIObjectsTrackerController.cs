using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.WorldMap.UI
{
	public class UIObjectsTrackerController : MonoBehaviour
	{
		[Serializable]
		private struct TrackedItem {
			public Transform WorldObject;
			public RectTransform UIObject;
		}

		public Camera WorldCamera;

		[SerializeField]
		private List<TrackedItem> m_TrackedItems = new List<TrackedItem>();

		public void StartTracking(Transform worldObject, RectTransform uiObject)
		{
			TrackedItem trackedItem = new TrackedItem() {
				WorldObject = worldObject,
				UIObject = uiObject,
			};

			uiObject.SetParent(transform, false);

			m_TrackedItems.Add(trackedItem);
		}

		public Transform StopTracking(Transform worldObject)
		{
			TrackedItem item = m_TrackedItems.First(ti => ti.WorldObject == worldObject);
			item.UIObject.SetParent(null);

			m_TrackedItems.Remove(item);

			return item.UIObject;
		}

		void LateUpdate()
		{
			foreach(TrackedItem item in m_TrackedItems) {
				Vector3 screenPos = WorldCamera.WorldToScreenPoint(item.WorldObject.position);

				if (item.UIObject.gameObject.activeSelf && screenPos != item.UIObject.position) {
					item.UIObject.position = screenPos;
				}
			}
		}
	}
}