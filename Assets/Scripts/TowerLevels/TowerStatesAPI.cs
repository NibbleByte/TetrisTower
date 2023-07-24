using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	/// <summary>
	/// Exposes methods for switching states and other useful operations (public methods to be linked in the UI).
	/// </summary>
	public class TowerStatesAPI : MonoBehaviour
	{
		public void PauseLevel()
		{
			Game.GameManager.Instance.SetGlobalState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			Game.GameManager.Instance.SetGlobalState(new TowerPlayState());
		}

		public void OpenOptions()
		{
			Game.GameManager.Instance.PushGlobalState(new TowerOptionsState());
		}

		public void QuitLevel()
		{
			var playthroughData = Game.GameManager.Instance.GameContext.CurrentPlaythrough;

			playthroughData.QuitLevel();

			Game.GameManager.Instance.SwitchLevelAsync(playthroughData.PrepareSupervisor());
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

				var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<GridLevelData>(TowerLevelDebugAPI.__DebugInitialTowerLevel, new Newtonsoft.Json.JsonSerializerSettings() {
					Converters = Saves.SaveManager.GetConverters(config),
					TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
				});

				playthroughData.ReplaceCurrentLevel(deserialized);
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