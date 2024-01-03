using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public void StartNewStory(PlaythroughTemplateBase template)
		{
			var gameContext = GameManager.Instance.GameContext;

			if (gameContext.StoryInProgress == null) {
				gameContext.SetCurrentPlaythrough(template);
				gameContext.SetStoryInProgress(gameContext.CurrentPlaythrough);
			} else {
				gameContext.SetCurrentPlaythrough(gameContext.StoryInProgress);
			}

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());

			// HACK: As we don't use states stack we can't use UIDisableCanvasGroupOnStatesChanging.
			//		 Manually clear the screen from any UI state, as scopes & hotkeys continue working while level is changing.
			var levelController = GameObject.FindObjectOfType<HomeScreenController>();
			if (levelController) {
				levelController.ClearState();
			}
		}

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