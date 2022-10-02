using DevLocker.GFrame.Input.UIScope;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Controller for the UITestScene used for testing out the UI + Input features of the GFrame.
	/// </summary>
	public class SampleUITesterController : MonoBehaviour
	{
		public void LoadMainMenu()
		{
#if GFRAME_ASYNC
			Game.SampleLevelsManager.Instance.SwitchLevelAsync(new MainMenu.SampleMainMenuLevelSupervisor());
#else
			Game.SampleLevelsManager.Instance.SwitchLevel(new MainMenu.SampleMainMenuLevelSupervisor());
#endif
		}
	}

}