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
			if (!Recording.GridLevelController.IsPaused && Recording.GridLevelController.LevelData.IsPlaying) {
				Recording.AddAndRun(new ReplayAction(ReplayActionType.Update, Time.deltaTime));
			}
		}
	}

}