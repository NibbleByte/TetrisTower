using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	/// <summary>
	/// Controls the MainMenu UI.
	/// </summary>
	public class SampleMainMenuController : MonoBehaviour
	{
		[Serializable]
		public struct StatePanelBinds
		{
			public SampleMainMenuState State;
			public GameObject Panel;
		}

		public SampleMainMenuState CurrentState;

		public StatePanelBinds[] StatePanels;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			foreach (var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			var nextState = CurrentState;
			CurrentState = null;

			SwitchState(nextState);
		}

		public void SwitchState(SampleMainMenuState state)
		{
			if (CurrentState) {
				var prevPanel = GetPanel(CurrentState);
				prevPanel.SetActive(false);
			}

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
		}

		public void LoadUITester()
		{
#if GFRAME_ASYNC
			m_PlayerContext.GetLevelManager().SwitchLevelAsync(new UITester.SampleUITesterLevelSupervisor());
#else
			m_PlayerContext.GetLevelManager().SwitchLevel(new UITester.SampleUITesterLevelSupervisor());
#endif
		}

		public void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		public GameObject GetPanel(SampleMainMenuState state)
		{
			foreach(var bind in StatePanels) {
				if (state == bind.State)
					return bind.Panel;
			}

			throw new NotImplementedException();
		}
	}

}