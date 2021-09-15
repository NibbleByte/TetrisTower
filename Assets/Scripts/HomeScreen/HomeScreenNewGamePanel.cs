using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			var gameContext = GameManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(gameContext.GameConfig.NewGameData);

			GameManager.Instance.SwitchLevel(new TowerLevels.TowerLevelSupervisor());
		}
	}

}