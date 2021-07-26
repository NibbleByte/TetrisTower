using System.Collections;
using UnityEngine;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Simple implementation of <see cref="ILevelLoadingScreen"/>.
	/// </summary>
	public class UISimpleCanvasGroupFader : MonoBehaviour, ILevelLoadingScreen
	{
		public float Duration = 0.25f;
		public bool TimeScaled = true;  // Should it be timeScale dependent or not.

		public bool HasShowFinished => isActiveAndEnabled && m_CanvasGroup.alpha == 1f;
		public bool HasHideFinished => !isActiveAndEnabled;

		private float m_StartTime;
		private float m_StartAlpha;
		private float m_EndAlpha;

		private float Now {
			get { return (TimeScaled) ? Time.time : Time.unscaledTime; }
		}

		private CanvasGroup m_CanvasGroup;

		public IEnumerator Show()
		{
			m_StartTime = Now;
			m_StartAlpha = 0.0f;
			m_EndAlpha = 1.0f;

			yield return UpdateProgress();
		}

		public IEnumerator Hide()
		{
			m_StartTime = Now;
			m_StartAlpha = 1.0f;
			m_EndAlpha = 0.0f;

			yield return UpdateProgress();

			gameObject.SetActive(false);
		}

		public void ShowInstantly()
		{
			m_CanvasGroup.alpha = 1f;
			gameObject.SetActive(true);
		}

		public void HideInstantly()
		{
			m_CanvasGroup.alpha = 0f;
			gameObject.SetActive(false);
		}

		private IEnumerator UpdateProgress()
		{
			m_CanvasGroup.alpha = m_StartAlpha;

			gameObject.SetActive(true);

			while (Now - m_StartTime < Duration) {
				float progress = (Now - m_StartTime) / Duration;

				progress = Mathf.Clamp01(progress);

				m_CanvasGroup.alpha = Mathf.Lerp(m_StartAlpha, m_EndAlpha, progress);

				yield return null;
			}

			m_CanvasGroup.alpha = m_EndAlpha;
		}

		void Awake()
		{
			m_CanvasGroup = GetComponent<CanvasGroup>();

			m_CanvasGroup.alpha = 0;

			gameObject.SetActive(false);
		}
	}
}