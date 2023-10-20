using System;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public enum TowerLevelUIState
	{
		None = 0,
		Play = 2,
		Paused = 4,
		LevelFinished = 6,
		Options = 8,
	}

	public class TowerLevelUIController : MonoBehaviour
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public TowerLevelUIState State;
			public GameObject Panel;
		}

		public TowerLevelUIState CurrentState = TowerLevelUIState.Play;

		public StatePanelBinds[] StatePanels;

		[Tooltip("Elements needed only when game is playing (i.e. not won / lost animation).")]
		public GameObject[] PlayingOnlyElements;

		[Tooltip("Elements to be displayed when replay is being recorded (i.e. normal game).")]
		public GameObject[] ReplayRecordingElements;
		[Tooltip("Elements to be displayed when replay is being played back.")]
		public GameObject[] ReplayPlaybackElements;

		void Awake()
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			SwitchState(CurrentState);
		}

		public void SwitchState(TowerLevelUIState state)
		{
			if (state == CurrentState)
				return;

			if (CurrentState != TowerLevelUIState.None) {
				var prevPanel = GetPanel(CurrentState);
				prevPanel.SetActive(false);
			}

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
		}

		public GameObject GetPanel(TowerLevelUIState state)
		{
			foreach (var bind in StatePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException();
		}


		public void SetIsLevelPlaying(bool isPlaying)
		{
			foreach(var element in PlayingOnlyElements) {
				element.SetActive(isPlaying);
			}
		}

		public void SetIsReplayPlaying(bool isReplayPlaying)
		{
			foreach(GameObject element in ReplayRecordingElements) {
				element.SetActive(!isReplayPlaying);
			}

			foreach(GameObject element in ReplayPlaybackElements) {
				element.SetActive(isReplayPlaying);
			}
		}
	}
}