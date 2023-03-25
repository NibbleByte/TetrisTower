using DevLocker.GFrame.Input;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Controller for the UITestScene used for testing out the UI + Input features of the GFrame.
	/// </summary>
	public class SampleUITesterController : MonoBehaviour
	{
		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		private void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

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