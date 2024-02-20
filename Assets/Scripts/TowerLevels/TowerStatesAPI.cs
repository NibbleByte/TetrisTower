using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using System;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Playthroughs;
using TetrisTower.TowerLevels.Replays;
using UnityEngine;

namespace TetrisTower.TowerLevels
{
	/// <summary>
	/// Exposes methods for switching states and other useful operations (public methods to be linked in the UI).
	/// </summary>
	public class TowerStatesAPI : MonoBehaviour, ILevelLoadedListener
	{
		private IPlaythroughData m_PlaythroughData;
		private IPlayerContext m_PlayerContext;
		private GameConfig m_Config;

		private bool m_IsReplay => m_PlaythroughData is ReplayPlaythroughData;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_Config);
			context.SetByType(out m_PlayerContext);
		}

		public void OnLevelUnloading()
		{
			// Just in case...
			m_PlaythroughData = null;
			m_Config = null;

			m_PlayerContext = null;
		}

		public void PauseLevel()
		{
			m_PlayerContext.StatesStack.SetState(new TowerPausedState());
		}

		public void ResumeLevel()
		{
			m_PlayerContext.StatesStack.SetState(m_IsReplay ? new TowerReplayPlaybackState() : new TowerPlayState());
		}

		public void OpenOptions()
		{
			m_PlayerContext.StatesStack.SetState(new TowerOptionsState());
		}

		public void QuitLevel()
		{
			SaveReplay(true);

			m_PlaythroughData.QuitLevel();

			GameManager.Instance.SwitchLevelAsync(m_PlaythroughData.PrepareSupervisor());
		}

		public void ExitToHomeScreen()
		{
			SaveReplay(true);

			GameManager.Instance.SwitchLevelAsync(new HomeScreen.HomeScreenLevelSupervisor());
		}

		public void RetryLevel()
		{
			SaveReplay(true);

			RetryLevelApply(m_PlaythroughData);

			GameManager.Instance.SwitchLevelAsync(m_PlaythroughData.PrepareSupervisor());
		}

		private void RetryLevelApply(IPlaythroughData playthroughData)
		{
			if (!string.IsNullOrEmpty(TowerLevelDebugAPI.__DebugInitialTowerLevel) && !m_IsReplay) {

				var deserialized = Saves.SavesManager.Deserialize<GridLevelData>(TowerLevelDebugAPI.__DebugInitialTowerLevel, m_Config);

				playthroughData.ReplaceCurrentLevel(deserialized);
				return;
			}

			Debug.Assert(playthroughData.ActiveTowerLevels.Any());

			playthroughData.RetryLevel();
		}

		public void GoToNextLevel()
		{
			SaveReplay(true);

			if (!GoToNextLevelApply(m_PlaythroughData))
				return;

			if (m_PlaythroughData.HaveFinishedLevels) {
				ExitToHomeScreen();
			} else {
				GameManager.Instance.SwitchLevelAsync(m_PlaythroughData.PrepareSupervisor());
			}

		}

		private static bool GoToNextLevelApply(IPlaythroughData playthroughData)
		{
			TowerLevelDebugAPI.__DebugInitialTowerLevel = string.Empty;

			Debug.Assert(playthroughData.ActiveTowerLevels.Any());

			if (!playthroughData.ActiveTowerLevels.Any(ld => ld.HasWon)) {
				Debug.LogError($"Trying to start the next level, while the player hasn't won the current one. Abort.");
				return false;
			}

			playthroughData.FinishLevel();

			return true;
		}

		public void ReplayLevel()
		{
			if (m_PlaythroughData.ActiveTowerLevels.Count == 0) {
				Debug.LogError($"Trying to replay level that isn't started.");
				return;
			}

			SaveReplay(true);

			IPlaythroughData nextPlaythroughData = m_PlaythroughData;

			if (!m_IsReplay) {
				var recording = m_PlayerContext.StatesStack.Context.FindByType<ReplayRecording>();
				if (!recording.HasEnding) {
					recording.EndReplayRecording();
				}
				recording = recording.Clone();

				nextPlaythroughData = new ReplayPlaythroughData(recording, m_PlaythroughData);
			}

			if (!m_PlaythroughData.ActiveTowerLevels.Any(ld => ld.IsPlaying) && m_PlaythroughData.ActiveTowerLevels.Any(ld => ld.HasWon)) {
				GoToNextLevelApply(m_PlaythroughData);
			} else {
				RetryLevelApply(m_PlaythroughData);
			}

			GameManager.Instance.SwitchLevelAsync(nextPlaythroughData.PrepareSupervisor());
		}

		public void SaveReplay()
		{
			SaveReplay(false);
		}

		private void SaveReplay(bool isAutoReplay)
		{
			// Level finished, already saved (probably)
			if (isAutoReplay && m_PlayerContext.StatesStack == null)
				return;

			ReplayRecording recording;
			if (m_PlaythroughData is ReplayPlaythroughData replayPlaythroughData) {

				// Don't auto-save replays in playback mode, as it was already saved.
				if (isAutoReplay)
					return;

				// Null controller as we are not gonna execute the recording.
				recording = replayPlaythroughData.GetReplayRecording(controller: null, fairy: null);
			} else {
				recording = m_PlayerContext.StatesStack.Context.FindByType<ReplayRecording>();
			}

			Saves.SavesManager.SaveReplay($"{DateTime.Now:yyyyMMdd_HHmmss} {m_PlaythroughData.ActiveTowerLevels[0].BackgroundScene.SceneName}", recording, isAutoReplay, m_Config);
		}

		void OnApplicationQuit()
		{
			SaveReplay(true);
		}
	}
}