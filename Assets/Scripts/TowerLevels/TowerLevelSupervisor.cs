using DevLocker.GFrame;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.Timing;
using DevLocker.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Playthroughs;
using TetrisTower.TowerLevels.Replays;
using TetrisTower.TowerUI;
using TetrisTower.Visuals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor
	{
		private IPlaythroughData m_PlaythroughData;
		private SceneReference m_OverrideScene;

		private int m_PlayersCount = 1;

		public TowerLevelSupervisor(IPlaythroughData playthroughData, int playersCount = 1)
		{
			m_PlaythroughData = playthroughData;
			m_PlayersCount = playersCount;
		}

		public void SetSceneOverride(SceneReference overrideScene)
		{
			m_OverrideScene = overrideScene;
		}

		public async Task LoadAsync()
		{
			GameContext gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			// Load levels + scenes per player. Scene index corresponds to the player index.
			for (int playerIndex = 0; playerIndex < m_PlayersCount; playerIndex++) {
				await LoadPlayerAsync(gameContext, playerIndex);
			}
		}

		public async Task LoadPlayerAsync(GameContext gameContext, int playerIndex)
		{
			GridLevelData levelData = playerIndex < m_PlaythroughData.ActiveTowerLevels.Count ? m_PlaythroughData.ActiveTowerLevels[playerIndex] : null;

			if (levelData == null) {
				// Prepare data for each player (including single player).
				// For multiplayer a special playthrough should be used that allows for multiple level setup calls.
				levelData = m_PlaythroughData.SetupCurrentTowerLevel(gameContext.GameConfig, m_OverrideScene);

				if (levelData == null) {
					CriticalError($"No available level.", true);
					return;
				}

				if (levelData.BackgroundScene.IsEmpty) {
					Debug.LogError($"No appropriate scene found in current level! Setting dev one.");
					levelData.BackgroundScene = new SceneReference("Assets/Scenes/_DevTowerScene.unity");

					bool isValidFallback = SceneUtility.GetBuildIndexByScenePath(levelData.BackgroundScene.ScenePath) >= 0;

					CriticalError($"Current level did not have scene specified. Loading fallback.", !isValidFallback);

					if (!isValidFallback) {
						return;
					}
				}

			// Debug level with preset data.
			} else if (levelData.BlocksSkinStack == null || levelData.BlocksSkinStack.IsEmpty) {

				// For debug saves, blocks may be missing. Fill them up with the defaults.
				levelData.BlocksSkinStack = new BlocksSkinStack(gameContext.GameConfig.DefaultBlocksSet, m_PlaythroughData.BlocksSet);
				levelData.BlocksSkinStack.Validate(gameContext.GameConfig.AssetsRepository, gameContext.GameConfig);
			}


			var backgroundScene = levelData.BackgroundScene;
			if (SceneManager.GetActiveScene().name != backgroundScene.SceneName || playerIndex != 0 || SceneManager.sceneCount > 1) {
				// Start by loading the first player scene with "Single" argument so it unloads the current one.
				var loadOp = SceneManager.LoadSceneAsync(backgroundScene.ScenePath, playerIndex == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive);
				while (!loadOp.isDone) await Task.Yield();
			}

			// Translate each player objects so they don't collide.
			if (playerIndex != 0) {
				var roots = SceneManager.GetSceneAt(playerIndex).GetRootGameObjects();
				foreach (GameObject root in roots) {
					root.transform.position += Vector3.right * (playerIndex % 2) * 500f + Vector3.forward * (playerIndex / 2) * 500f;
				}
			}


			List<Transform> restPoints;
			Visuals.Effects.FairyMatchingController fairy;
			Camera camera;

			var levelController = FindObjectOfType<GridLevelController>(playerIndex);
			if (levelController == null) {
				var placeholder = FindGameObjectWithTag(GameTags.TowerPlaceholderTag, playerIndex);
				if (placeholder == null) {
					throw new Exception($"Scene {SceneManager.GetActiveScene().name} has missing level controller and placeholder. Cannot load current level.");
				}

				levelController = GameObject.Instantiate<GridLevelController>(gameContext.GameConfig.TowerLevelController, placeholder.transform.position, placeholder.transform.rotation);
				SceneManager.MoveGameObjectToScene(levelController.gameObject, SceneManager.GetSceneAt(playerIndex));
				levelController.name = $"{gameContext.GameConfig.TowerLevelController.name} [{playerIndex}]";

				Light overrideBlocksLight = placeholder.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));
				if (overrideBlocksLight) {
					Light blocksLight = levelController.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));
					overrideBlocksLight.transform.SetParent(blocksLight.transform.parent, false);
					GameObject.DestroyImmediate(blocksLight.gameObject);
				}

				camera = levelController.GetComponentInChildren<Camera>();
				var overrideCamera = placeholder.GetComponentInChildren<Camera>();
				if (overrideCamera) {
					// Move the parent object, since it's position is updated on changing screen orientation.
					overrideCamera.transform.parent.SetParent(camera.transform.parent.parent, false);
					GameObject.DestroyImmediate(camera.transform.parent.gameObject);
					camera = overrideCamera;
				}

				Transform[] overrideDecors = placeholder.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.TowerDecors)).ToArray();
				if (overrideDecors.Length != 0) {
					Transform[] decors = levelController.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.TowerDecors)).ToArray();

					foreach(Transform overrideDecor in overrideDecors) {
						// Child of tower decors were already moved - don't change their hierarchy.
						if (overrideDecor.IsChildOf(placeholder.transform)) {
							overrideDecor.SetParent(levelController.transform, true);
						}
					}

					foreach(Transform decor in decors) {
						GameObject.DestroyImmediate(decor.gameObject);
					}
				}

				var overrideEffects = placeholder.GetComponentInChildren<Visuals.Effects.EffectsOverrider>();
				if (overrideEffects) {
					// It doesn't replace original objects, but still needs to be rescued from destruction.
					overrideEffects.transform.SetParent(levelController.transform, false);

					overrideEffects.Override(levelController.GetComponentInChildren<ConeVisualsGrid>());
					overrideEffects.Override(levelController.GetComponentInChildren<TowerConeVisualsController>());
				}


				restPoints = levelController.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.FairyRestPoint)).ToList();
				List<Transform> overrideRestPoints = placeholder.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.FairyRestPoint)).ToList();
				if (overrideRestPoints.Count != 0) {

					if (restPoints.Count != 0) {
						foreach (Transform overrideDecor in overrideRestPoints) {
							overrideDecor.SetParent(restPoints[0].parent, true); // Assume all have the same parent.
						}

						foreach (Transform decor in restPoints) {
							GameObject.DestroyImmediate(decor.gameObject);
						}

						restPoints = overrideRestPoints;

					} else {
						Debug.LogError("No rest points found in the instantiated level controller!", levelController);
					}
				}

				fairy = levelController.GetComponentInChildren<Visuals.Effects.FairyMatchingController>();
				var overrideFairy = placeholder.GetComponentInChildren<Visuals.Effects.FairyMatchingController>();
				if (overrideFairy) {
					overrideFairy.transform.SetParent(fairy.transform.parent, false);
					GameObject.DestroyImmediate(fairy.gameObject);
					fairy = overrideFairy;
				}

				// Clean any leftovers in the placeholder (for example, temporary camera).
				placeholder.transform.DestroyChildren(true);

			} else {

				restPoints = levelController.GetComponentsInChildren<Transform>(true).Where(t => t.CompareTag(GameTags.FairyRestPoint)).ToList();
				fairy = levelController.GetComponentInChildren<Visuals.Effects.FairyMatchingController>();

				camera = levelController.GetComponentInChildren<Camera>();
			}

			SetupLights(levelController, playerIndex);

			SetupCamera(camera, playerIndex);

			var uiController = FindObjectOfType<TowerLevelUIController>(playerIndex);
			if (uiController == null) {
				GameObject[] uiPrefabs = Platforms.PlatformsUtils.IsMobileOrSimulator
					? gameContext.GameConfig.UIPrefabsMobile
					: gameContext.GameConfig.UIPrefabs
					;

				foreach (GameObject prefab in uiPrefabs) {
					var instance = GameObject.Instantiate<GameObject>(prefab);
					instance.name = $"{prefab.name} [{playerIndex}]";

					SceneManager.MoveGameObjectToScene(instance.gameObject, SceneManager.GetSceneAt(playerIndex));

					if (uiController == null) {
						uiController = instance.GetComponent<TowerLevelUIController>();
					}
				}
			}




			WiseTiming timing;
			LevelReplayPlayback playbackComponent = null;
			LevelReplayRecorder recordComponent = null;
			System.Random visualsRandom;

			if (m_PlaythroughData is ReplayPlaythroughData replayPlaythroughData) {
				playbackComponent = levelController.gameObject.AddComponent<LevelReplayPlayback>();
				playbackComponent.PlaybackRecording = replayPlaythroughData.GetReplayRecording(levelController, fairy);
				playbackComponent.PlaybackRecording.ApplyFairyPositions(fairy, restPoints);
				playbackComponent.enabled = false;

				visualsRandom = new Xoshiro.PRNG32.XoShiRo128starstar(playbackComponent.PlaybackRecording.InitialVisualsRandomSeed);

				timing = playbackComponent.Timing;
			} else {
				int seed = levelData.RandomInitialLevelSeed;
				visualsRandom = new Xoshiro.PRNG32.XoShiRo128starstar(seed);

				recordComponent = levelController.gameObject.AddComponent<LevelReplayRecorder>();
				recordComponent.Recording.SaveInitialState(levelData, gameContext.GameConfig, fairy, restPoints, seed);
				recordComponent.Recording.GridLevelController = levelController;
				recordComponent.Recording.Fairy = fairy;
				recordComponent.enabled = false;

				timing = recordComponent.Timing;
			}

			uiController.SetIsReplayPlaying(playbackComponent != null);

			//
			// Setup Input
			//
			var playerContext = uiController.GetComponent<PlayerContextUIRootObject>();
			var playthroughPlayer = PlaythroughPlayer.Create(gameContext.GameConfig, levelController, camera, uiController.gameObject);
			playthroughPlayer.EventSystem.name = $"{gameContext.GameConfig.GameInputPrefab.name} [{playerIndex}]";
			m_PlaythroughData.AssignPlayer(playthroughPlayer, levelData);

			// Suppress global input.
			//PlayerContextUIRootObject.GlobalPlayerContext.EventSystem.gameObject.SetActive(false);

			//
			// Setup Player Context
			//
			var behaviours = FindObjectsOfType<MonoBehaviour>(playerIndex);

			playerContext.CreatePlayerStack(
				gameContext,
				gameContext.GameConfig,
				playthroughPlayer.PlayerControls,
				gameContext.Options,
				m_PlaythroughData,
				levelData,
				levelController,
				uiController,
				timing,
				playbackComponent,
				recordComponent?.Recording, // Provide it for recording, otherwise, don't need it.
				new MultiObjectivesPresenter(behaviours.OfType<ObjectivesUIController>()),
				behaviours.OfType<GreetMessageUIController>().FirstOrDefault(),
				behaviours.OfType<FlashMessageUIController>().FirstOrDefault(),
				behaviours.OfType<ConeVisualsGrid>().First(),
				behaviours.OfType<TowerConeVisualsController>().First(),
				behaviours.OfType<Visuals.Effects.FairyMatchingController>().FirstOrDefault(),
				behaviours.OfType<TowerStatesAPI>().First(),
				behaviours.OfType<ILostAnimationExecutor>().ToArray()	// Tower level prefab OR scene ones.
				);


			// Init before others.
			levelController.Init(levelData, timing);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(playerContext.StatesStack.Context);
			}

			// Other visuals depend on this, so init it first.
			behaviours.OfType<TowerConeVisualsController>().First().Init(playerContext.StatesStack.Context, visualsRandom);

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(playerContext.StatesStack.Context);
			}

			foreach(var objective in levelController.LevelData.Objectives) {
				objective.OnPostLevelLoaded(playerContext.StatesStack.Context);
			}


			if (playbackComponent) {
				playbackComponent.enabled = true;
			} else {
				recordComponent.enabled = true;
			}

			playerContext.StatesStack.SetState(playbackComponent ? new TowerReplayPlaybackState() : new TowerPlayState());

			if (levelData.IsPlaying) {
				// If save came with available matches, or pending actions, do them.
				var pendingActions = GameGridEvaluation.Evaluate(levelData.Grid, levelData.Rules);
				if (pendingActions.Count > 0) {
					levelController.StartRunActions(pendingActions);

					while (levelController.AreGridActionsRunning) {
						await Task.Yield();
					}
				}
			}
		}

		public Task UnloadAsync()
		{
			foreach(var player in m_PlaythroughData.ActivePlayers) {
				player.EventSystem.gameObject.SetActive(false);
			}

			// Restore the global input.
			PlayerContextUIRootObject.GlobalPlayerContext.EventSystem.gameObject.SetActive(true);


			for (int playerIndex = 0; playerIndex < m_PlayersCount; ++playerIndex) {

				var behaviours = FindObjectsOfType<MonoBehaviour>(playerIndex);

				var levelController = behaviours.OfType<GridLevelController>().First();

				foreach (Objective objective in levelController.LevelData.Objectives) {
					objective.OnPreLevelUnloading();
				}

				foreach (var behaviour in behaviours) {
					var listener = behaviour as ILevelLoadedListener;
					if (listener != null) {
						listener.OnLevelUnloading();
					}

					// Skip DontDestroyOnLoads.
					if (behaviour.gameObject.scene.buildIndex != -1) {
						// Make sure no coroutines leak to the next level (in case target scene is the same, objects won't be reloaded).
						behaviour.StopAllCoroutines();
					}
				}

				behaviours.OfType<TowerConeVisualsController>().FirstOrDefault()?.Deinit();

				GameObject.DestroyImmediate(levelController.GetComponent<LevelReplayRecorder>());
				GameObject.DestroyImmediate(levelController.GetComponent<LevelReplayPlayback>());
			}

			return Task.CompletedTask;
		}

		private static T FindObjectOfType<T>(int playerIndex) where T : Component
		{
			return SceneManager.GetSceneAt(playerIndex)
				.GetRootGameObjects()
				.SelectMany(root => root.EnumerateComponentsInChildren<T>(true))
				.FirstOrDefault();
		}

		private static T[] FindObjectsOfType<T>(int playerIndex) where T : Component
		{
			return SceneManager.GetSceneAt(playerIndex)
				.GetRootGameObjects()
				.SelectMany(root => root.EnumerateComponentsInChildren<T>(true))
				.ToArray();
		}

		private static GameObject FindGameObjectWithTag(string tag, int playerIndex)
		{
			return SceneManager.GetSceneAt(playerIndex)
				.GetRootGameObjects()
				.SelectMany(root => root.EnumerateComponentsInChildren<Transform>(true))
				.Where(t => t.CompareTag(tag))
				.Select(t => t.gameObject)
				.FirstOrDefault();
		}

		private static void SetupLights(GridLevelController levelController, int playerIndex)
		{
			var blocksLight = levelController.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));

			if (blocksLight) {
				blocksLight.cullingMask = GameLayers.BlocksMask;

				var levelLights = FindObjectsOfType<Light>(playerIndex);
				foreach(Light light in levelLights) {
					if (light != blocksLight) {
						light.cullingMask &= light.GetComponentInParent<Visuals.Effects.FairyMatchingController>()
							? ~0 // Fairy lights up everything
							: ~GameLayers.BlocksMask // Light environment without the blocks.
							;

						// Only one directional light allowed.
						if (playerIndex != 0 && light.type == LightType.Directional) {
							light.enabled = false;
						}
					}
				}
			}
		}

		private void SetupCamera(Camera camera, int playerIndex)
		{
			// Only one audio listener allowed.
			if (playerIndex != 0) {
				GameObject.DestroyImmediate(camera.GetComponentInChildren<AudioListener>(true));
			}

			Rect rect = m_PlayersCount switch {
				// Full screen
				1 => new Rect(0f, 0f, 1f, 1f),

				// Side by side
				2 => new Rect(playerIndex * (1f / m_PlayersCount), 0f, 1f / m_PlayersCount, 1f),
				3 => new Rect(playerIndex * (1f / m_PlayersCount), 0f, 1f / m_PlayersCount, 1f),

				// 4 corners
				4 => new Rect((playerIndex % 2) * (2f / m_PlayersCount), ((3 - playerIndex) / 2) * (2f / m_PlayersCount), 2f / m_PlayersCount, 2f / m_PlayersCount),

				_ => throw new NotSupportedException($"{m_PlayersCount} players not supported."),
			};

			camera.rect = rect;
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