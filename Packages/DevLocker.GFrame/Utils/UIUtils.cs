using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.Utils
{
	/// <summary>
	/// UI helper functions.
	/// </summary>
	public static class UIUtils
	{
		private static IDictionary m_CanvasesRegister;
		private static List<ICanvasElement> m_LayoutRebuildList;

		/// <summary>
		/// Force recalculate content size fitters and layout groups properly - from bottom to top.
		/// https://forum.unity.com/threads/content-size-fitter-refresh-problem.498536/#post-6857996
		/// </summary>
		public static void ForceRecalclulateLayouts(RectTransform rootTransform)
		{
			if (rootTransform == null || !rootTransform.gameObject.activeSelf) {
				return;
			}

			foreach (RectTransform child in rootTransform) {
				ForceRecalclulateLayouts(child);
			}

			var layoutGroup = rootTransform.GetComponent<LayoutGroup>();
			var contentSizeFitter = rootTransform.GetComponent<ContentSizeFitter>();
			if (layoutGroup != null) {
				layoutGroup.SetLayoutHorizontal();
				layoutGroup.SetLayoutVertical();
			}

			if (contentSizeFitter != null) {
				LayoutRebuilder.ForceRebuildLayoutImmediate(rootTransform);
			}
		}

		/// <summary>
		/// Returns true if CanvasGroup parents allow this object to be clicked (i.e. <see cref="CanvasGroup.blocksRaycasts"/> and <see cref="CanvasGroup.interactable"/> are enabled).
		/// </summary>
		public static bool IsClickable(GameObject gameObject)
		{
			var group = gameObject.GetComponentInParent<CanvasGroup>(true);

			if (group == null)
				return true;

			while(group) {

				if (group.enabled) {
					if (!group.blocksRaycasts || !group.interactable)
						return false;

					if (group.ignoreParentGroups)
						break;
				}

				Transform parent = group.transform.parent;
				if (parent == null)
					break;

				group = group.transform.parent.GetComponentInParent<CanvasGroup>(true);
			}

			return true;
		}

		/// <summary>
		/// Checks the Unity UGUI system if there is a layout rebuild pending to happen at the end of the frame.
		/// </summary>
		public static bool IsLayoutRebuildPending()
		{
			if (m_LayoutRebuildList == null) {
				// Use reflection to get the internal list used for marked objects for layout rebuild.
				// List is stored by reference, so it should be up to date.
				// Idea by https://forum.unity.com/threads/callback-for-when-a-canvas-rebuild-happens.525100/

				try {
					FieldInfo buildQueueField = typeof(CanvasUpdateRegistry).GetField("m_LayoutRebuildQueue", BindingFlags.NonPublic | BindingFlags.Instance);
					object buildQueue = buildQueueField.GetValue(CanvasUpdateRegistry.instance);
					FieldInfo buildListField = buildQueue.GetType().GetField("m_List", BindingFlags.NonPublic | BindingFlags.Instance);
					m_LayoutRebuildList = (List<ICanvasElement>)buildListField.GetValue(buildQueue);
				}
				catch (Exception) {
					// I guess the API changed and this check is no longer valid.
					m_LayoutRebuildList = new List<ICanvasElement>();
				}
			}

			return m_LayoutRebuildList.Count > 0;
		}

		/// <summary>
		/// Returns all active canvases from the <see cref="GraphicRegistry"/>. It's fast!
		/// </summary>
		public static IEnumerable<Canvas> GetAllCanvases()
		{
			if (m_CanvasesRegister == null) {
				// Use reflection to get the internal dictionary from the registry.
				// This will give us quick access to all the canvases.

				try {
					FieldInfo graphicsField = typeof(GraphicRegistry).GetField("m_Graphics", BindingFlags.NonPublic | BindingFlags.Instance);
					m_CanvasesRegister = (IDictionary) graphicsField.GetValue(GraphicRegistry.instance);
				}
				catch (Exception) {
					// I guess the API changed and this call is no longer valid.
					m_CanvasesRegister = new Dictionary<Canvas, object>();
				}
			}

			return m_CanvasesRegister.Keys.OfType<Canvas>();
		}
	}
}
