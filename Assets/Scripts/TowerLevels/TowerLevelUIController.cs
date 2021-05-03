using System.Collections;
using System.Collections.Generic;
using TetrisTower.Core;
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
			LevelSupervisorsManager.Instance.SetLevelState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			LevelSupervisorsManager.Instance.SetLevelState(new TowerPlayState());
		}

		public void ExitToHomeScreen()
		{
			LevelSupervisorsManager.Instance.SwitchLevel(new HomeScreen.HomeScreenLevelSupervisor());
		}
	}
}