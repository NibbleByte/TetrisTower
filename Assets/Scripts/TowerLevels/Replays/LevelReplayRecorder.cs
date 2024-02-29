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

		public ReplayActionsRecording PlayerRecording;

		void Update()
		{
			if (PlayerRecording.GridLevelController.IsPaused)
				return;

			if (PlayerRecording.GridLevelController.LevelData.IsPlaying && !PlayerRecording.HasEnding) {
				// Update fairy pos so we can record it's parameters for desync checks.
				// This is needed as while matching animations are happening, ReplayActionType.Update doesn't update meaningful data.
				PlayerRecording.AddAndRun(ReplayActionType.FairyPos, Time.deltaTime);

				PlayerRecording.AddAndRun(ReplayActionType.Update, Time.deltaTime);
			} else {
				Timing.UpdateCoroutines(Time.deltaTime);
			}
		}
	}

}