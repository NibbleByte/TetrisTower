using DevLocker.GFrame.Timing;
using UnityEngine;

namespace TetrisTower.TowerLevels.Replays
{
	/// <summary>
	/// Ticks the level time.
	/// Also responsible for the replay recording / playback.
	/// </summary>
	public class LevelReplayRecorder : MonoBehaviour
	{
		public readonly WiseTiming Timing = new WiseTiming();

		public readonly ReplayRecording Recording = new ReplayRecording();

		void Update()
		{
			if (Recording.GridLevelController.IsPaused)
				return;

			if (Recording.GridLevelController.LevelData.IsPlaying && !Recording.HasEnding) {
				Recording.AddAndRun(ReplayActionType.Update, Time.deltaTime);
			} else {
				Timing.UpdateCoroutines(Time.deltaTime);
			}
		}
	}

}