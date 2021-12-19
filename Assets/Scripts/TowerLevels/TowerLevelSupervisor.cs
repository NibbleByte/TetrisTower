using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using DevLocker.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public async Task LoadAsync()
		{
			GameContext gameContext = GameManager.Instance.GameContext;
			PlaythroughData playthroughData = gameContext.CurrentPlaythrough;
			Debug.Assert(playthroughData != null);

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (playthroughData.TowerLevel == null) {
				playthroughData.TowerLevel = playthroughData.Levels[playthroughData.CurrentLevelIndex].GenerateTowerLevelData();
			}

			var backgroundScene = playthroughData.TowerLevel.BackgroundScene;
			if (SceneManager.GetActiveScene().name != backgroundScene.SceneName) {
				var loadOp = SceneManager.LoadSceneAsync(backgroundScene.ScenePath, LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}

			var levelController = GameObject.FindObjectOfType<GridLevelController>();
			if (levelController == null) {
				var placeholder = GameObject.FindGameObjectWithTag(GameConfig.TowerPlaceholderTag);
				if (placeholder == null) {
					throw new Exception($"Scene {SceneManager.GetActiveScene().name} has missing level controller and placeholder. Cannot load level {playthroughData.CurrentLevelIndex} of {playthroughData.Levels.Length}.");
				}

				// Clean any leftovers in the placeholder (for example, temporary camera).
				placeholder.transform.DestroyChildren(true);

				levelController = GameObject.Instantiate<GridLevelController>(gameContext.GameConfig.TowerLevelController, placeholder.transform.position, placeholder.transform.rotation);
			}

			levelController.Init(playthroughData.TowerLevel);

			var uiController = GameObject.FindObjectOfType<UI.TowerLevelUIController>(true);
			if (uiController == null) {
				foreach(GameObject prefab in gameContext.GameConfig.UIPrefabs) {
					var instance = GameObject.Instantiate<GameObject>(prefab);

					if (uiController == null) {
						uiController = instance.GetComponent<UI.TowerLevelUIController>();
					}
				}
			}

			var matchSequenceScoreDisplayer = GameObject.FindObjectOfType<UI.MatchSequenceScoreUIController>(true);

			StatesStack = new LevelStateStack(
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				gameContext.CurrentPlaythrough,
				levelController,
				uiController,
				matchSequenceScoreDisplayer
				);


			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(StatesStack.ContextReferences);
			}

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(StatesStack.ContextReferences);
			}

			await StatesStack.SetStateAsync(new TowerPlayState());

			// If save came with available matches, or pending actions, do them.
			var pendingActions = Logic.GameGridEvaluation.Evaluate(gameContext.CurrentPlaythrough.TowerLevel.Grid, gameContext.CurrentPlaythrough.TowerLevel.Rules);
			if (pendingActions.Count > 0) {
				levelController.StartCoroutine(levelController.RunActions(pendingActions));

				while(levelController.AreGridActionsRunning) {
					await Task.Yield();
				}
			}
		}

		public Task UnloadAsync()
		{
			var levelListeners = GameObject.FindObjectsOfType<MonoBehaviour>(true).OfType<ILevelLoadedListener>();
			foreach (var listener in levelListeners) {
				listener.OnLevelUnloading();
			}

			return Task.CompletedTask;
		}
	}
}