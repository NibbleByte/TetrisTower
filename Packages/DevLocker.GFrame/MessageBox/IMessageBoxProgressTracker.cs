namespace DevLocker.GFrame.MessageBox
{
	/// <summary>
	/// Used by processing UI controller to display progress bar.
	/// The confirm button will be disabled until the progress bar reaches 100%.
	/// </summary>
	public interface IMessageBoxProgressTracker
	{
		float PollFrequency { get; }	// In seconds
		bool IsReady { get; }

		float CalcNormalizedProgress();
	}

	public class FakeProcessingProgressTracker : IMessageBoxProgressTracker
	{
		public FakeProcessingProgressTracker(float secondsToProcess = 10f)
		{
			SecondsToProcess = secondsToProcess;
		}

		public float SecondsToProcess;
		public float ProcessedSeconds { get; private set; } = -1f;

		public float PollFrequency => 1f;
		public bool IsReady => ProcessedSeconds >= SecondsToProcess;

		public float CalcNormalizedProgress()
		{
			ProcessedSeconds++;
			return ProcessedSeconds / SecondsToProcess;
		}
	}
}