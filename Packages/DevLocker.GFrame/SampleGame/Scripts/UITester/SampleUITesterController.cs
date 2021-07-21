using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Controller for the UITestScene used for testing out the UI + Input features of the GFrame.
	/// Can switch between "pages" responsible for different types of tests.
	/// </summary>
	public class SampleUITesterController : MonoBehaviour
	{
		public int CurrentPage;

		public GameObject[] PagePanels;

		void Awake()
		{
			foreach(var panel in PagePanels) {
				panel.SetActive(false);
			}

			var nextPage = CurrentPage;
			CurrentPage = -1;

			SwitchPage(nextPage);
		}

		public void SwitchPage(int page)
		{
			if (CurrentPage >= 0) {
				PagePanels[CurrentPage].SetActive(false);
			}

			CurrentPage = page;

			PagePanels[CurrentPage].SetActive(true);
		}

		public void SwitchToNextPage()
		{
			SwitchPage((CurrentPage + 1) % PagePanels.Length);
		}

		public void SwitchToPrevPage()
		{
			var page = CurrentPage - 1;
			if (page < 0) {
				page += PagePanels.Length;
			}

			SwitchPage(page);
		}

		public void LoadMainMenu()
		{
			LevelsManager.Instance.SwitchLevel(new MainMenu.SampleMainMenuLevelSupervisor());
		}
	}

}