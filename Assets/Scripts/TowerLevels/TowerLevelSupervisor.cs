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

		private SceneReference m_OverrideScene;

		public TowerLevelSupervisor() { }
		public TowerLevelSupervisor(SceneReference overrideScene)
		{
			m_OverrideScene = overrideScene;
		}

		public async Task LoadAsync()
		{
			GameContext gameContext = GameManager.Instance.GameContext;
			PlaythroughData playthroughData = gameContext.CurrentPlaythrough;
			Debug.Assert(playthroughData != null);

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (playthroughData.TowerLevel == null) {

				playthroughData.SetupCurrentLevel(gameContext.GameConfig, m_OverrideScene);

				if (playthroughData.TowerLevel == null) {
					CriticalError($"No available level.", true);
					return;
				}

				if (playthroughData.TowerLevel.BackgroundScene.IsEmpty) {
					Debug.LogError($"No appropriate scene found in current level! Setting dev one.");
					playthroughData.TowerLevel.BackgroundScene = new SceneReference("Assets/Scenes/_DevTowerScene.unity");

					bool isValidFallback = SceneUtility.GetBuildIndexByScenePath(playthroughData.TowerLevel.BackgroundScene.ScenePath) >= 0;

					CriticalError($"Current level did not have scene specified. Loading fallback.", !isValidFallback);

					if (!isValidFallback) {
						return;
					}
				}

			} else if (playthroughData.TowerLevel.BlocksSkinSet == null) {

				// For debug saves, blocks may be missing. Fill them up with the defaults.
				playthroughData.TowerLevel.BlocksSkinSet = playthroughData.BlocksSet ?? gameContext.GameConfig.DefaultBlocksSet;
				playthroughData.TowerLevel.BlocksSkinSet.Validate(gameContext.GameConfig.AssetsRepository, gameContext.GameConfig);
			}

			var backgroundScene = playthroughData.TowerLevel.BackgroundScene;
			if (SceneManager.GetActiveScene().name != backgroundScene.SceneName) {
				var loadOp = SceneManager.LoadSceneAsync(backgroundScene.ScenePath, LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}

			var levelController = GameObject.FindObjectOfType<GridLevelController>();
			if (levelController == null) {
				var placeholder = GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag);
				if (placeholder == null) {
					throw new Exception($"Scene {SceneManager.GetActiveScene().name} has missing level controller and placeholder. Cannot load current level.");
				}

				levelController = GameObject.Instantiate<GridLevelController>(gameContext.GameConfig.TowerLevelController, placeholder.transform.position, placeholder.transform.rotation);

				var overrideBlocksLight = placeholder.transform.GetComponentInChildren<Light>();
				if (overrideBlocksLight) {
					var blocksLight = levelController.GetComponentInChildren<Light>();
					overrideBlocksLight.transform.SetParent(blocksLight.transform.parent, false);
					GameObject.DestroyImmediate(blocksLight.gameObject);
				}


				var overrideCamera = placeholder.GetComponentInChildren<Camera>();
				if (overrideCamera) {
					// Move the parent object, since it's position is updated on changing screen orientation.
					var camera = levelController.GetComponentInChildren<Camera>();
					overrideCamera.transform.parent.SetParent(camera.transform.parent.parent, false);
					GameObject.DestroyImmediate(camera.transform.parent.gameObject);
				}

				Transform[] overrideDecors = placeholder.transform.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.TowerDecors)).ToArray();
				if (overrideDecors.Length != 0) {
					Transform[] decors = levelController.transform.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.TowerDecors)).ToArray();

					foreach(Transform overrideDecor in overrideDecors) {
						overrideDecor.SetParent(levelController.transform, true);
					}

					foreach(Transform decor in decors) {
						GameObject.DestroyImmediate(decor.gameObject);
					}
				}

				// Clean any leftovers in the placeholder (for example, temporary camera).
				placeholder.transform.DestroyChildren(true);
			}

			SetupLights(levelController);

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

			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			StatesStack = new LevelStateStack(
				gameContext,
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				gameContext.CurrentPlaythrough,
				levelController,
				uiController,
				behaviours.OfType<UI.FlashMessageUIController>().FirstOrDefault(),
				behaviours.OfType<ConeVisualsGrid>().First(),
				behaviours.OfType<TowerConeVisualsController>().First(),
				behaviours.OfType<ILostAnimationExecutor>().ToArray()	// Tower level prefab OR scene ones.
				);


			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(StatesStack.ContextReferences);
			}

			// Other visuals depend on this, so init it first.
			behaviours.OfType<TowerConeVisualsController>().First().Init(StatesStack.ContextReferences);

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(StatesStack.ContextReferences);
			}

			await StatesStack.SetStateAsync(new TowerPlayState());

			ShowRulesMessage(playthroughData.TowerLevel);

			if (gameContext.CurrentPlaythrough.TowerLevel.IsPlaying) {
				GridLevelData levelData = gameContext.CurrentPlaythrough.TowerLevel;
				// If save came with available matches, or pending actions, do them.
				var pendingActions = GameGridEvaluation.Evaluate(levelData.Grid, levelData.Rules);
				if (pendingActions.Count > 0) {
					levelController.StartCoroutine(levelController.RunActions(pendingActions));

					while (levelController.AreGridActionsRunning) {
						await Task.Yield();
					}
				}
			}
		}

		public Task UnloadAsync()
		{
			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			foreach (var behaviour in behaviours) {
				var listener = behaviour as ILevelLoadedListener;
				if (listener != null) {
					listener.OnLevelUnloading();
				}

				// Make sure no coroutines leak to the next level (in case target scene is the same, objects won't be reloaded).
				behaviour.StopAllCoroutines();
			}

			behaviours.OfType<TowerConeVisualsController>().FirstOrDefault()?.Deinit();

			return Task.CompletedTask;
		}

		private static void SetupLights(GridLevelController levelController)
		{
			var blocksLight = levelController.GetComponentInChildren<Light>();

			if (blocksLight) {
				blocksLight.cullingMask = GameLayers.BlocksMask;

				var levelLights = GameObject.FindObjectsOfType<Light>();
				foreach(Light light in levelLights) {
					if (light != blocksLight) {
						light.cullingMask &= ~GameLayers.BlocksMask;
					}
				}
			}
		}

		private void ShowRulesMessage(GridLevelData levelData)
		{
			var flashMessagesController = StatesStack.ContextReferences.TryFindByType<UI.FlashMessageUIController>();
			if (flashMessagesController && !levelData.Rules.IsObjectiveAllMatchTypes) {
				string message = "";
				if (levelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Horizontal)) {
					message += "Horizontal ";
				}
				if (levelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Vertical)) {
					message += "Vertical ";
				}
				if (levelData.Rules.ObjectiveType.HasFlag(MatchScoringType.Diagonals)) {
					message += "Diagonals ";
				}

				message += "Only!";

				flashMessagesController.ShowMessage(message, false);
			}
		}

		private void CriticalError(string errorMessage, bool fallbackToHomescreen)
		{
			MessageBox.Instance.ShowSimple(
				"Level Error",
				errorMessage,
				MessageBoxIcon.Error,
				MessageBoxButtons.OK,
				() => {
					if (fallbackToHomescreen)
						GameManager.Instance.SwitchLevelAsync(new HomeScreen.HomeScreenLevelSupervisor());
				},
				this
			);
		}
	}
}