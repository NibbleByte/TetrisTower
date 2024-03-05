using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenAPI : MonoBehaviour
	{
		public HomeScreenController HomeScreenController;

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

		public void StartPlayOnLocation(TowerLocationPickerController locationPicker)
		{
			var gameContext = GameManager.Instance.GameContext;

			var levelParams = new[] { locationPicker.PickedLevelParam };
			IPlaythroughData playthroughData = locationPicker.TargetPlaythroughTemplate.GeneratePlaythroughData(gameContext.GameConfig, levelParams);
			gameContext.SetCurrentPlaythrough(playthroughData);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}

		public void StartPlayWithSceneOverride(TowerLocationPickerController locationPicker)
		{
			var gameContext = GameManager.Instance.GameContext;

			var levelParams = locationPicker.TargetPlaythroughTemplate.GetAllLevels().Select(originalParam => {

				var levelParam = Saves.SavesManager.Clone<LevelParamData>(originalParam, gameContext.GameConfig);
				levelParam.BackgroundScene = levelParam.BackgroundSceneMobile = locationPicker.PickedLevelParam.GetAppropriateBackgroundScene();
				return levelParam;
			});

			// Don't use the TowerLevelSupervisor scene override parameter, as it would be lost on replay quit.
			IPlaythroughData playthroughData = locationPicker.TargetPlaythroughTemplate.GeneratePlaythroughData(gameContext.GameConfig, levelParams);
			gameContext.SetCurrentPlaythrough(playthroughData);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}
	}

}