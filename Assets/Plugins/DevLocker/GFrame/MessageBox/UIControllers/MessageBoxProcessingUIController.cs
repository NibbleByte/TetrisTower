using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.MessageBox.UIControllers
{
	/// <summary>
	/// Displays an in-game prompt with message and processing progress bar.
	/// The confirm button will be disabled until the progress bar reaches 100%.
	/// </summary>
	public class MessageBoxProcessingUIController : MessageBoxSimpleUIController
	{
		public Slider ProgressBar;
		public MessageBoxUIText ProgressBarText;

		public string ProgressBarTextPrefix = "";
		public string ProgressBarTextSuffix = "%";

		private Coroutine m_UpdateProgressCrt;

		public override void Show(MessageData data)
		{
			base.Show(data);

			SetProgress(m_ShownData.ProgressTracker.CalcNormalizedProgress());

			var button = GetActiveConfirmButton();
			button.interactable = false;

			m_UpdateProgressCrt = StartCoroutine(UpdateProgress());
		}

		public override void Close()
		{
			base.Close();

			StopCoroutine(m_UpdateProgressCrt);
		}

		private IEnumerator UpdateProgress()
		{
			while (!m_ShownData.ProgressTracker.IsReady) {
				yield return new WaitForSeconds(m_ShownData.ProgressTracker.PollFrequency);

				SetProgress(m_ShownData.ProgressTracker.CalcNormalizedProgress());
			}

			SetProgress(1f);

			var button = GetActiveConfirmButton();
			button.interactable = true;
		}

		private void SetProgress(float progress)
		{
			if (ProgressBar != null) {
				ProgressBar.value = progress;
			}

			ProgressBarText.Text = ProgressBarTextPrefix + Mathf.Round(ProgressBar.value * 100) + ProgressBarTextSuffix;
		}

		// TODO
		//protected override bool OnInputEventInternal(InputEvent ev)
		//{
		//	if (ev.Type == InputEventType.Pressed) {
		//		if (ev.Name == "Submit" || ev.Name == "ConfirmAction") {
		//			// Stop ConfirmAction if progress is not ready yet.
		//			if (!_shownData.ProgressTracker.IsReady) {
		//				return true;
		//			}
		//		}
		//	}
		//
		//	return base.OnInputEventInternal(ev);
		//}
	}
}
