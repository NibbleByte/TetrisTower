using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.Utils
{
	/// <summary>
	/// UI helper functions.
	/// </summary>
	public static class UIUtils
	{
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
	}
}
