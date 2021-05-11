using DevLocker.GameFrame;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	public enum TowerLevelUIState
	{
		Play = 0,
		Paused = 4,
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

		void Awake()
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			SwitchState(CurrentState);
		}

		public void SwitchState(TowerLevelUIState state)
		{
			var prevPanel = GetPanel(CurrentState);
			prevPanel.SetActive(false);

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



		public void PauseLevel()
		{
			LevelsManager.Instance.SetLevelState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			LevelsManager.Instance.SetLevelState(new TowerPlayState());
		}

		public void OpenOptions()
		{
			LevelsManager.Instance.SetLevelState(new TowerOptionsState());
		}

		public void ExitToHomeScreen()
		{
			LevelsManager.Instance.SwitchLevel(new HomeScreen.HomeScreenLevelSupervisor());
		}
	}
}