using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using DevLocker.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.Visuals;
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

			Light overrideBlocksLight = null;

			var levelController = GameObject.FindObjectOfType<GridLevelController>();
			if (levelController == null) {
				var placeholder = GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag);
				if (placeholder == null) {
					throw new Exception($"Scene {SceneManager.GetActiveScene().name} has missing level controller and placeholder. Cannot load level {playthroughData.CurrentLevelIndex} of {playthroughData.Levels.Length}.");
				}

				// Will use it as overrides in a bit. Keep it alive.
				overrideBlocksLight = placeholder.transform.GetComponentInChildren<Light>();
				if (overrideBlocksLight) {
					overrideBlocksLight.transform.parent = null;
				}

				// Clean any leftovers in the placeholder (for example, temporary camera).
				placeholder.transform.DestroyChildren(true);

				levelController = GameObject.Instantiate<GridLevelController>(gameContext.GameConfig.TowerLevelController, placeholder.transform.position, placeholder.transform.rotation);
			}

			SetupLights(levelController, overrideBlocksLight);

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

			StatesStack = new LevelStateStack(
				gameContext,
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				gameContext.CurrentPlaythrough,
				levelController,
				uiController,
				GameObject.FindObjectOfType<UI.MatchSequenceScoreUIController>(true),
				GameObject.FindObjectOfType<UI.FlashMessageUIController>(true)
				);


			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(StatesStack.ContextReferences);
			}

			// Other visuals depend on this, so init it first.
			behaviours.OfType<TowerConeVisualsController>().First().Init(StatesStack.ContextReferences);

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
			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelUnloading();
			}

			behaviours.OfType<TowerConeVisualsController>().First().Deinit();

			return Task.CompletedTask;
		}

		private static void SetupLights(GridLevelController levelController, Light overrideBlocksLight)
		{
			var blocksLight = levelController.GetComponentInChildren<Light>();

			if (blocksLight) {
				if (overrideBlocksLight) {
					overrideBlocksLight.transform.parent = blocksLight.transform.parent;
					GameObject.DestroyImmediate(blocksLight.gameObject);

					blocksLight = overrideBlocksLight;
				}

				blocksLight.cullingMask = GameLayers.BlocksMask;
				blocksLight.lightmapBakeType = LightmapBakeType.Realtime;

				var levelLights = GameObject.FindObjectsOfType<Light>();
				foreach(Light light in levelLights) {
					if (light != blocksLight) {
						light.cullingMask &= ~GameLayers.BlocksMask;
					}
				}
			}
		}
	}
}