using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Example tween "system" that fades in canvas group on enable.
	/// </summary>
	public class SampleUITesterTweenFader : MonoBehaviour
	{
		public CanvasGroup Group;
		public float Duration = 0.2f;

		void OnEnable()
		{
			StartCoroutine(FadeIn());
		}

		IEnumerator FadeIn()
		{
			Group.alpha = 0f;
			Group.blocksRaycasts = false;

			float startTime = Time.time;

			while(Time.time < startTime + Duration) {

				Group.alpha = Mathf.Lerp(0f, 1f, (Time.time - startTime) / Duration);

				yield return null;
			}

			Group.alpha = 1f;
			Group.blocksRaycasts = true;
		}
	}
}