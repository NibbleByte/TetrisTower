using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
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

		void Awake()
		{
			foreach(var bind in StatePanels) {
				bind.Panel.SetActive(false);
			}

			SwitchState(CurrentState);
		}

		public void SwitchState(SampleMainMenuState state)
		{
			var prevPanel = GetPanel(CurrentState);
			prevPanel.SetActive(false);

			CurrentState = state;

			var nextPanel = GetPanel(state);
			nextPanel.SetActive(true);
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