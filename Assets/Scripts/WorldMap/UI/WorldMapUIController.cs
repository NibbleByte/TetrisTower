using System;
using UnityEngine;

namespace TetrisTower.WorldMap.UI
{
	public enum WorldMapUIState
	{
		None = 0,
		Play = 2,
		Paused = 4,
		Options = 8,
	}

	public class WorldMapUIController : MonoBehaviour
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public WorldMapUIState State;
			public GameObject Panel;
		}

		public WorldMapUIState CurrentState = WorldMapUIState.Play;

		public StatePanelBinds[] StatePanels;

		void Awake()
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			SwitchState(CurrentState);
		}

		public void SwitchState(WorldMapUIState state)
		{
			if (state == CurrentState)
				return;

			if (CurrentState != WorldMapUIState.None) {
				var prevPanel = GetPanel(CurrentState);
				prevPanel.SetActive(false);
			}

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
		}

		public GameObject GetPanel(WorldMapUIState state)
		{
			foreach (var bind in StatePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException();
		}

		public void PauseLevel()
		{
			Game.GameManager.Instance.SetLevelState(new WorldMapPausedState());
		}

		public void ResumeLevel()
		{
			Game.GameManager.Instance.SetLevelState(new WorldMapPlayState());
		}

		public void OpenOptions()
		{
			// TODO: Add Options state? Shared for Tower & World levels?
			throw new NotSupportedException();
			//Game.GameManager.Instance.PushGlobalState(new TowerOptionsState());
		}

		public void ExitToHomeScreen()
		{
			Game.GameManager.Instance.SwitchLevelAsync(new HomeScreen.HomeScreenLevelSupervisor());
		}
	}
}