using DevLocker.GFrame;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public HomeScreenController HomeScreenController;
		public HomeScreenState LoadingState;

		public void StartNewGame(PlaythroughTemplate template)
		{
			var gameContext = GameManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(template);

			GameManager.Instance.SwitchLevelAsync(new TowerLevels.TowerLevelSupervisor());

			HomeScreenController.SwitchState(LoadingState);
		}
	}

}