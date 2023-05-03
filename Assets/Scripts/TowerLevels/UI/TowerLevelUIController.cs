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

		[Tooltip("Elements needed only when game is playing (i.e. not won / lost animation).")]
		public GameObject[] PlayingOnlyElements;

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

		public void PauseLevel()
		{
			Game.GameManager.Instance.PushGlobalState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			Game.GameManager.Instance.SetLevelState(new TowerPlayState());
		}

		public void OpenOptions()
		{
			Game.GameManager.Instance.PushGlobalState(new TowerOptionsState());
		}

		public void ExitToHomeScreen()
		{
			Game.GameManager.Instance.SwitchLevelAsync(new HomeScreen.HomeScreenLevelSupervisor());
		}

		public static void RetryLevel()
		{
			var playthroughData = Game.GameManager.Instance.GameContext.CurrentPlaythrough;

			if (!string.IsNullOrEmpty(TowerLevelDebugAPI.__DebugInitialTowerLevel)) {
				var config = Game.GameManager.Instance.GameContext.GameConfig;

				playthroughData.ReplaceCurrentLevel(Newtonsoft.Json.JsonConvert.DeserializeObject<GridLevelData>(TowerLevelDebugAPI.__DebugInitialTowerLevel, Saves.SaveManager.GetConverters(config)));
				Game.GameManager.Instance.SwitchLevelAsync(playthroughData.PrepareSupervisor());
				return;
			}

			Debug.Assert(playthroughData.TowerLevel != null);

			playthroughData.RetryLevel();

			Game.GameManager.Instance.SwitchLevelAsync(playthroughData.PrepareSupervisor());
		}

		public void GoToNextLevel()
		{
			TowerLevelDebugAPI.__DebugInitialTowerLevel = string.Empty;

			var playthroughData = Game.GameManager.Instance.GameContext.CurrentPlaythrough;
			Debug.Assert(playthroughData.TowerLevel != null);

			if (!playthroughData.TowerLevel.HasWon) {
				Debug.LogError($"Trying to start the next level, while the player hasn't won the current one. Abort.");
				return;
			}

			playthroughData.FinishLevel();

			if (playthroughData.HaveFinishedLevels) {
				ExitToHomeScreen();
			} else {
				Game.GameManager.Instance.SwitchLevelAsync(playthroughData.PrepareSupervisor());
			}

		}
	}
}