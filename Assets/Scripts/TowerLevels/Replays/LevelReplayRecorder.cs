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
				// Update fairy pos so we can record it's parameters for desync checks.
				// This is needed as while matching animations are happening, ReplayActionType.Update doesn't update meaningful data.
				Recording.AddAndRun(ReplayActionType.FairyPos, Time.deltaTime);

				Recording.AddAndRun(ReplayActionType.Update, Time.deltaTime);
			} else {
				Timing.UpdateCoroutines(Time.deltaTime);
			}
		}
	}

}