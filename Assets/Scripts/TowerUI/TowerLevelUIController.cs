using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public enum TowerLevelUIPlayMode
	{
		NormalPlay,
		PVPPlay,
		Replay,
	}

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

		public GameObject SinglePlayerDecorations;
		public GameObject MultiPlayerDecorations;

		public TowerLevelUIState CurrentState = TowerLevelUIState.Play;

		public StatePanelBinds[] StatePanels;
		public StatePanelBinds[] PVPStatePanels;
		public StatePanelBinds[] ReplayStatePanels;

		[Tooltip("Elements needed only when game is playing (i.e. not won / lost animation).")]
		public GameObject[] PlayingOnlyElements;

		public TowerLevelUIPlayMode PlayMode { get; private set; }

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			bool isSinglePlayer = context.FindByType<IPlaythroughData>().IsSinglePlayer;
			SinglePlayerDecorations.SetActive(isSinglePlayer);
			MultiPlayerDecorations.SetActive(!isSinglePlayer);

			foreach (var bind in StatePanels.Concat(PVPStatePanels).Concat(ReplayStatePanels)) {
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
			var statePanels = PlayMode switch {
				TowerLevelUIPlayMode.NormalPlay => this.StatePanels,
				TowerLevelUIPlayMode.PVPPlay => PVPStatePanels,
				TowerLevelUIPlayMode.Replay => ReplayStatePanels,
				_ => throw new NotImplementedException(),
			};

			foreach (var bind in statePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException($"Not supported {state} for {PlayMode}");
		}


		public void SetIsLevelPlaying(bool isPlaying)
		{
			foreach(var element in PlayingOnlyElements) {
				element.SetActive(isPlaying);
			}
		}

		public void SetPlayMode(TowerLevelUIPlayMode playMode)
		{
			PlayMode = playMode;
		}
	}
}