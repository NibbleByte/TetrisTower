using DevLocker.GFrame.SampleGame.Game;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
#if GFRAME_ASYNC
			SampleLevelsManager.Instance.SwitchLevelAsync(new Play.SamplePlaySupervisor());
#else
			SampleLevelsManager.Instance.SwitchLevel(new Play.SamplePlaySupervisor());
#endif
		}
	}

}