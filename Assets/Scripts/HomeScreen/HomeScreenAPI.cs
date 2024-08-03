using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Modes;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.HomeScreen
{

	public class HomeScreenAPI : MonoBehaviour
	{
		public HomeScreenController HomeScreenController;

		private TowerModesHighScoresDatabase HighScoresDatabase => GameManager.Instance.GetManager<TowerModesHighScoresDatabase>();

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

		public void StartSequence(TowerLocationsSequenceController sequenceController)
		{
			var gameContext = GameManager.Instance.GameContext;

			var allLevels = sequenceController.TargetPlaythroughTemplate.GetAllLevels().ToArray();
			allLevels = Saves.SavesManager.Clone<LevelParamData[]>(allLevels, gameContext.GameConfig);
			foreach (var levelParam in allLevels) {
				ModifyParamDifficulty(levelParam, sequenceController.PickedDifficulty);
			}

			IPlaythroughData playthroughData = sequenceController.TargetPlaythroughTemplate.GeneratePlaythroughData(gameContext.GameConfig, allLevels);
			playthroughData.SetupRandomGenerator(sequenceController.PickedSeed);

			gameContext.SetCurrentPlaythrough(playthroughData);

			HighScoresDatabase.StartTowerMode(sequenceController.TargetPlaythroughTemplate, sequenceController.PickedDifficulty, sequenceController.PickedSeed);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}

		public void StartPlayOnLocation(TowerLocationPickerController locationPicker)
		{
			var gameContext = GameManager.Instance.GameContext;

			var pickedParam = Saves.SavesManager.Clone<WorldMapLevelParamData>(locationPicker.PickedLevelParam, gameContext.GameConfig);

			ModifyParamDifficulty(pickedParam, locationPicker.PickedDifficulty);

			var levelParams = new[] { pickedParam };
			IPlaythroughData playthroughData = locationPicker.TargetPlaythroughTemplate.GeneratePlaythroughData(gameContext.GameConfig, levelParams);
			playthroughData.SetupRandomGenerator(locationPicker.PickedSeed);

			gameContext.SetCurrentPlaythrough(playthroughData);

			// Don't start mode for PVP - doesn't make sense.
			//HighScoresDatabase.StartTowerMode(locationPicker.TargetPlaythroughTemplate, pickedParam.LevelID, locationPicker.PickedDifficulty, locationPicker.PickedSeed);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}

		// DEPRECATED: this was used for Endless play, but it replaced the whole level definition, losing the gameplay elements.
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

		public void StartEndlessPlay(TowerLocationPickerController locationPicker)
		{
			var gameContext = GameManager.Instance.GameContext;

			// locationPicker.TargetPlaythroughTemplate is not needed here as we can recreate the objectives ourselves in the code. For now.

			var pickedParam = Saves.SavesManager.Clone<WorldMapLevelParamData>(locationPicker.PickedLevelParam, gameContext.GameConfig);

			pickedParam.GreetMessage = "Endless!";
			pickedParam.Objectives = new List<Objective>{ new TowerObjectives.PlaceOutside_Objective() { PlacingOutsideIsWin = true } };

			ModifyParamDifficulty(pickedParam, locationPicker.PickedDifficulty);

			var levelParams = new[] { pickedParam };

			// Don't use the TowerLevelSupervisor scene override parameter, as it would be lost on replay quit.
			IPlaythroughData playthroughData = locationPicker.TargetPlaythroughTemplate.GeneratePlaythroughData(gameContext.GameConfig, levelParams);
			playthroughData.SetupRandomGenerator(locationPicker.PickedSeed);

			gameContext.SetCurrentPlaythrough(playthroughData);
			HighScoresDatabase.StartTowerMode(locationPicker.TargetPlaythroughTemplate, pickedParam.LevelID, locationPicker.PickedDifficulty, locationPicker.PickedSeed);

			GameManager.Instance.SwitchLevelAsync(gameContext.CurrentPlaythrough.PrepareSupervisor());
		}

		private void ModifyParamDifficulty(LevelParamData param, TowerDifficulty difficulty)
		{
			param.InitialSpawnBlockTypesCount = Mathf.Max(param.InitialSpawnBlockTypesCount + (int)difficulty, 3);
			param.Rules.FallSpeedNormalized += ((int)difficulty) * param.Rules.FallSpeedupPerAction * 3f;
		}
	}

}