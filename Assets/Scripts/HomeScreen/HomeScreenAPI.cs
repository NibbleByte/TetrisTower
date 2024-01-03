using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenAPI : MonoBehaviour
	{
		public HomeScreenController HomeScreenController;

		public PVPPlaythroughTemplate PVPTemplate;

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
		}

		public void StartNewGame(PlaythroughTemplateBase template)
		{
			var gameContext = GameManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(template);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}

		public void StartPVPGame(TowerLocationPickerController locationPicker)
		{
			var gameContext = GameManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(PVPTemplate);

			((PVPPlaythroughData)gameContext.CurrentPlaythrough).SetCurrentLevel(locationPicker.PickedLevelParam);
			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}
	}

}