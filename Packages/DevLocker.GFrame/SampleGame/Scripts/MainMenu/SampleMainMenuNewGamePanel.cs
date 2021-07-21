using DevLocker.GFrame.SampleGame.Game;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			LevelsManager.Instance.SwitchLevel(new Play.SamplePlaySupervisor());
		}
	}

}