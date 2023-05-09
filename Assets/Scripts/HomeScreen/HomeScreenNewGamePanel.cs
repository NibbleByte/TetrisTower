using DevLocker.GFrame;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public void StartNewGame(PlaythroughTemplateBase template)
		{
			var gameContext = GameManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(template);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());

			// HACK: As we don't use states stack we can't use UIDisableCanvasGroupOnStatesChanging.
			//		 Manually clear the screen from any UI state, as scopes & hotkeys continue working while level is changing.
			var levelController = GameObject.FindObjectOfType<HomeScreenController>();
			if (levelController) {
				levelController.ClearState();
			}
		}
	}

}