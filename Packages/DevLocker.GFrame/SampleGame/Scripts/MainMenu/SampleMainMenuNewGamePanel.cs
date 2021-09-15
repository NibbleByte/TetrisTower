using DevLocker.GFrame.SampleGame.Game;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			SampleLevelsManager.Instance.SwitchLevel(new Play.SamplePlaySupervisor());
		}
	}

}