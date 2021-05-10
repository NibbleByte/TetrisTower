using DevLocker.GameFrame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelUIController : MonoBehaviour
	{
		public GameObject PlayPanel;
		public GameObject PausedPanel;

		public void ShowPausedPanel(bool pause)
		{
			PlayPanel.SetActive(!pause);
			PausedPanel.SetActive(pause);
		}


		public void PauseLevel()
		{
			LevelsManager.Instance.SetLevelState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			LevelsManager.Instance.SetLevelState(new TowerPlayState());
		}

		public void ExitToHomeScreen()
		{
			LevelsManager.Instance.SwitchLevel(new HomeScreen.HomeScreenLevelSupervisor());
		}
	}
}