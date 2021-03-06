using System;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.Play
{
	public enum PlayUIState
	{
		None = 0,
		Play = 2,
		Paused = 4,
		Options = 8,
	}

	public class SamplePlayUIController : MonoBehaviour
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public PlayUIState State;
			public GameObject Panel;
		}

		public PlayUIState CurrentState = PlayUIState.Play;

		public GameObject JumperModePanel;
		public GameObject ChopperModePanel;

		public Text ModeLabel;

		public StatePanelBinds[] StatePanels;

		void Awake()
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			SwitchState(CurrentState, true);
		}

		public void SwitchState(PlayUIState state, bool? jumperMode = null)
		{
			if (jumperMode.HasValue) {
				JumperModePanel?.SetActive(jumperMode.Value);
				ChopperModePanel?.SetActive(!jumperMode.Value);

				if (ModeLabel) {
					ModeLabel.text = $"Player Mode: {(jumperMode.Value ? "Jumper" : "Chopper")}";
				}
			}

			if (state == CurrentState)
				return;

			if (CurrentState != PlayUIState.None) {
				var prevPanel = GetPanel(CurrentState);
				prevPanel.SetActive(false);
			}

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
		}

		public GameObject GetPanel(PlayUIState state)
		{
			foreach (var bind in StatePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException();
		}


		public void PauseLevel()
		{
			// Will be popped by UI.
			LevelsManager.Instance.PushLevelState(new SamplePlayPausedState());
		}

		public void OpenOptions()
		{
			// Will be popped by UI.
			LevelsManager.Instance.PushLevelState(new SamplePlayOptionsState());
		}

		public void ExitToMainMenu()
		{
			LevelsManager.Instance.SwitchLevel(new MainMenu.SampleMainMenuLevelSupervisor());
		}
	}
}