using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Simple implementation of <see cref="ILevelLoadingScreen"/>.
	/// </summary>
	public class UISimpleCanvasGroupFader_LoadingScreen : MonoBehaviour, ILevelLoadingScreen
	{
		public float Duration = 0.25f;
		public bool TimeScaled = true;  // Should it be timeScale dependent or not.

		[Tooltip("How many frames should loading screen wait before starting hide animation. First few frames performance may be unstable resulting in chopppy or skipped animation.")]
		public int WaitFramesBeforeHide = 2;

		public bool HasShowFinished => isActiveAndEnabled && m_CanvasGroup.alpha == 1f;
		public bool HasHideFinished => !isActiveAndEnabled;

		private float m_StartTime;
		private float m_StartAlpha;
		private float m_EndAlpha;

		private float Now {
			get { return (TimeScaled) ? Time.time : Time.unscaledTime; }
		}

		private CanvasGroup m_CanvasGroup;

#if GFRAME_ASYNC
		public async Task ShowAsync()
#else
		public IEnumerator Show()
#endif
		{
			m_StartTime = Now;
			m_StartAlpha = 0.0f;
			m_EndAlpha = 1.0f;

#if GFRAME_ASYNC
			await UpdateProgressAsync();
#else
			yield return UpdateProgress();
#endif
		}

#if GFRAME_ASYNC
		public async Task HideAsync()
#else
		public IEnumerator Hide()
#endif
		{
			int startFrame = Time.frameCount;
			while(WaitFramesBeforeHide > 0 && Time.frameCount - startFrame < WaitFramesBeforeHide) {
#if GFRAME_ASYNC
				await Task.Yield();
#else
				yield return null;
#endif
			}

			m_StartTime = Now;
			m_StartAlpha = 1.0f;
			m_EndAlpha = 0.0f;

#if GFRAME_ASYNC
			await UpdateProgressAsync();
#else
			yield return UpdateProgress();
#endif

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

#if GFRAME_ASYNC
		private async Task UpdateProgressAsync()
#else
		private IEnumerator UpdateProgress()
#endif
		{
			m_CanvasGroup.alpha = m_StartAlpha;

			gameObject.SetActive(true);

			while (Now - m_StartTime < Duration) {
				float progress = (Now - m_StartTime) / Duration;

				progress = Mathf.Clamp01(progress);

				m_CanvasGroup.alpha = Mathf.Lerp(m_StartAlpha, m_EndAlpha, progress);

#if GFRAME_ASYNC
				await Task.Yield();
#else
				yield return null;
#endif
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