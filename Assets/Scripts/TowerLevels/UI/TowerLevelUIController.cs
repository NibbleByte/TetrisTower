using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels.UI
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



		public void PauseLevel()
		{
			Game.GameManager.Instance.PushLevelState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			Game.GameManager.Instance.SetLevelState(new TowerPlayState());
		}

		public void OpenOptions()
		{
			Game.GameManager.Instance.PushLevelState(new TowerOptionsState());
		}

		public void ExitToHomeScreen()
		{
			Game.GameManager.Instance.SwitchLevel(new HomeScreen.HomeScreenLevelSupervisor());
		}

		public void GoToNextLevel()
		{
			var playthroughData = Game.GameManager.Instance.GameContext.CurrentPlaythrough;
			Debug.Assert(playthroughData.TowerLevel != null);

			if (playthroughData.TowerLevel.RunningState != TowerLevelRunningState.Won) {
				Debug.LogError($"Trying to start the next level, while the player hasn't won the current one. Abort.");
				return;
			}

			playthroughData.FinishLevel();

			if (playthroughData.HaveFinishedLevels) {
				ExitToHomeScreen();
			} else {
				Game.GameManager.Instance.SwitchLevel(new TowerLevelSupervisor());
			}

		}
	}
}