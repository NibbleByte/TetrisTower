using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenController : MonoBehaviour, ILevelLoadedListener
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public HomeScreenState State;
			public GameObject Panel;
		}

		public HomeScreenState CurrentState;

		public StatePanelBinds[] StatePanels;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			var nextState = CurrentState;
			CurrentState = null;

			SwitchState(nextState);
		}

		public void OnLevelUnloading()
		{
			// HACK: As we don't use states stack we can't use UIDisableCanvasGroupOnStatesChanging.
			//		 Manually clear the screen from any UI state, as scopes & hotkeys continue working while level is changing.
			ClearState();
		}

		public void SwitchState(HomeScreenState state)
		{
			if (CurrentState) {
				var prevPanel = GetPanel(CurrentState);
				prevPanel.SetActive(false);
			}

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
		}

		public void ClearState()
		{
			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			CurrentState = null;
		}

		public void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public GameObject GetPanel(HomeScreenState state)
		{
			foreach(var bind in StatePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException();
		}
	}

}