using DevLocker.GFrame.SampleGame.Game;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			var gameContext = (SampleGameContext) LevelsManager.Instance.GameContext;

			// TODO: Play supervisor
			//LevelsManager.Instance.SwitchLevel(new TowerLevels.TowerLevelSupervisor());
		}
	}

}