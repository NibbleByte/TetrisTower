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

		protected override bool ValidateResponse(MessageBoxResponse result)
		{
			switch (result) {
				case MessageBoxResponse.Yes:
				case MessageBoxResponse.OK:
				case MessageBoxResponse.Retry:
					return m_ShownData.ProgressTracker.IsReady;
			}

			return true;
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
	}
}
