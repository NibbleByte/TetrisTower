using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.Utils
{
	/// <summary>
	/// UI helper functions.
	/// </summary>
	public static class UIUtils
	{
		// Force recalculate content size fitters and layout groups properly - from bottom to top.
		// https://forum.unity.com/threads/content-size-fitter-refresh-problem.498536/#post-6857996
		public static void ForceRecalclulateLayouts(RectTransform transform)
		{
			if (transform == null || !transform.gameObject.activeSelf) {
				return;
			}

			foreach (RectTransform child in transform) {
				ForceRecalclulateLayouts(child);
			}

			var layoutGroup = transform.GetComponent<LayoutGroup>();
			var contentSizeFitter = transform.GetComponent<ContentSizeFitter>();
			if (layoutGroup != null) {
				layoutGroup.SetLayoutHorizontal();
				layoutGroup.SetLayoutVertical();
			}

			if (contentSizeFitter != null) {
				LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
			}
		}
	}
}
