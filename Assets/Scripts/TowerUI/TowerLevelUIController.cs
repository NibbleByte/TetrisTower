using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using System.Linq;
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

	public class TowerLevelUIController : MonoBehaviour, ILevelLoadedListener
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public TowerLevelUIState State;
			public GameObject Panel;
		}

		public TowerLevelUIState CurrentState = TowerLevelUIState.Play;

		public StatePanelBinds[] StatePanels;
		public StatePanelBinds[] ReplayStatePanels;

		[Tooltip("Elements needed only when game is playing (i.e. not won / lost animation).")]
		public GameObject[] PlayingOnlyElements;

		public bool IsReplayPlaying { get; private set; }

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			foreach (var bind in StatePanels.Concat(ReplayStatePanels)) {
				bind.Panel.SetActive(false);
			}
		}

		public void OnLevelUnloading()
		{

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
			var statePanels = IsReplayPlaying ? ReplayStatePanels : StatePanels;

			foreach (var bind in statePanels) {
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
			IsReplayPlaying = isReplayPlaying;
		}
	}
}