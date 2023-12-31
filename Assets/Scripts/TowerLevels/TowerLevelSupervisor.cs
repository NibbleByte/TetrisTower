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
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor
	{
		private IPlaythroughData m_PlaythroughData;
		private SceneReference m_OverrideScene;

		private int m_PlayersCount = 1;
		private TowerLevelPlayersManager m_PlayersManager;

		private struct StartData
		{
			public PlaythroughPlayer Player;
			public MonoBehaviour[] Behaviours;
			public System.Random VisualsRandom;
		}

		public TowerLevelSupervisor(IPlaythroughData playthroughData, int playersCount = 1)
		{
			m_PlaythroughData = playthroughData;
			m_PlayersCount = playersCount;
			m_PlayersManager = new TowerLevelPlayersManager(m_PlaythroughData, m_PlayersCount);
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

			var allPlayersStartData = new List<StartData>();

			// Load levels + scenes per player. Scene index corresponds to the player index.
			for (int playerIndex = 0; playerIndex < m_PlayersCount; playerIndex++) {
				var startData = await LoadPlayerAsync(gameContext, playerIndex);

				// Error happened - should be already handled. Maybe use exceptions instead?
				if (startData.Player == null)
					return;

				// Carry over the found data to the next step so we don't search for them again.
				allPlayersStartData.Add(startData);
			}

			// Finish up levels setup after all scenes were loaded.
			await StartLevels(allPlayersStartData);
		}

		private async Task<StartData> LoadPlayerAsync(GameContext gameContext, int playerIndex)
		{
			GridLevelData levelData = playerIndex < m_PlaythroughData.ActiveTowerLevels.Count ? m_PlaythroughData.ActiveTowerLevels[playerIndex] : null;

			if (levelData == null) {
				// Prepare data for each player (including single player).
				// For multiplayer a special playthrough should be used that allows for multiple level setup calls.
				levelData = m_PlaythroughData.SetupCurrentTowerLevel(gameContext.GameConfig, m_OverrideScene);

				if (levelData == null) {
					CriticalError($"No available level.", true);
					return default;
				}

				if (levelData.BackgroundScene.IsEmpty) {
					Debug.LogError($"No appropriate scene found in current level! Setting dev one.");
					levelData.BackgroundScene = new SceneReference("Assets/Scenes/_DevTowerScene.unity");

					bool isValidFallback = SceneUtility.GetBuildIndexByScenePath(levelData.BackgroundScene.ScenePath) >= 0;

					CriticalError($"Current level did not have scene specified. Loading fallback.", !isValidFallback);

					if (!isValidFallback) {
						return default;
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

			var uiCanvases = FindObjectsOfType<Canvas>(playerIndex);
			var uiController = FindObjectOfType<TowerLevelUIController>(playerIndex);
			if (uiController == null) {
				GameObject[] uiPrefabs = Platforms.PlatformsUtils.IsMobileOrSimulator
					? gameContext.GameConfig.UIPrefabsMobile
					: gameContext.GameConfig.UIPrefabs
					;

				uiCanvases = new Canvas[uiPrefabs.Length];

				for(int i = 0; i < uiPrefabs.Length; ++i) {
					GameObject prefab = uiPrefabs[i];
					var instance = GameObject.Instantiate<GameObject>(prefab);
					instance.name = $"{prefab.name} [{playerIndex}]";

					SceneManager.MoveGameObjectToScene(instance.gameObject, SceneManager.GetSceneAt(playerIndex));

					uiCanvases[i] = instance.GetComponent<Canvas>();

					if (uiController == null) {
						uiController = instance.GetComponent<TowerLevelUIController>();
					}
				}
			}

			if (uiCanvases.Length < 2 || uiCanvases.Any(c => c == null)) {
				Debug.LogError("Parts of the UI are missing!");
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

			if (m_PlayersCount > 1) {
				uiController.SetPlayMode(TowerLevelUIPlayMode.PVPPlay);
			} else if (playbackComponent != null) {
				uiController.SetPlayMode(TowerLevelUIPlayMode.Replay);
			} else {
				uiController.SetPlayMode(TowerLevelUIPlayMode.NormalPlay);
			}

			//
			// Setup Player
			//
			var playerContext = uiController.GetComponent<PlayerContextUIRootObject>();
			var playthroughPlayer = m_PlayersManager.SetupPlayer(gameContext.GameConfig, playerIndex, levelController, levelData, camera, playerContext, uiCanvases);
			playthroughPlayer.EventSystem.name = $"{gameContext.GameConfig.GameInputPrefab.name} [{playerIndex}]";

			//
			// Setup Player Context
			//
			var behaviours = FindObjectsOfType<MonoBehaviour>(playerIndex);

			playerContext.CreatePlayerStack(
				gameContext,
				gameContext.GameConfig,
				playthroughPlayer,
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

			return new StartData { Player = playthroughPlayer, Behaviours = behaviours, VisualsRandom = visualsRandom };
		}

		private async Task StartLevels(List<StartData> allPlayersStartData)
		{
			m_PlayersManager.SetupPlayersInput();

			// After all players have loaded their scenes, start the game.
			for (int playerIndex = 0; playerIndex < allPlayersStartData.Count; playerIndex++) {
				StartData startData = allPlayersStartData[playerIndex];
				PlaythroughPlayer player = startData.Player ;
				MonoBehaviour[] behaviours = startData.Behaviours;

				//
				// IMPORTANT: Loading and Loaded listeners should be called after all players were setup.
				//

				foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
					await listener.OnLevelLoadingAsync(player.PlayerContext.StatesStack.Context);
				}

				// Other visuals depend on this, so init it first.
				behaviours.OfType<TowerConeVisualsController>().First().Init(player.PlayerContext.StatesStack.Context, startData.VisualsRandom, GameLayers.BlocksLayer(playerIndex));

				foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
					listener.OnLevelLoaded(player.PlayerContext.StatesStack.Context);
				}

				foreach (var objective in player.LevelData.Objectives) {
					objective.OnPostLevelLoaded(player.PlayerContext.StatesStack.Context);
				}

				var playbackComponent = player.LevelController.GetComponent<LevelReplayPlayback>();
				var recordComponent = player.LevelController.GetComponent<LevelReplayRecorder>();

				if (playbackComponent) {
					playbackComponent.enabled = true;
				} else {
					recordComponent.enabled = true;
				}

				player.PlayerContext.StatesStack.SetState(playbackComponent ? new TowerReplayPlaybackState() : new TowerPlayState());

				if (player.LevelData.IsPlaying) {
					// If save came with available matches, or pending actions, do them.
					var pendingActions = GameGridEvaluation.Evaluate(player.LevelData.Grid, player.LevelData.Rules);
					if (pendingActions.Count > 0) {
						player.LevelController.StartRunActions(pendingActions);

						while (player.LevelController.AreGridActionsRunning) {
							await Task.Yield();
						}
					}
				}
			}
		}

		public Task UnloadAsync()
		{
			m_PlayersManager.Dispose();

			for (int playerIndex = 0; playerIndex < m_PlayersCount; ++playerIndex) {

				var behaviours = FindObjectsOfType<MonoBehaviour>(playerIndex);

				var levelController = behaviours.OfType<GridLevelController>().First();

				levelController.DeInit();

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
				// Set layer per player so their blocks directional lights don't mix.
				blocksLight.cullingMask = GameLayers.BlocksMask(playerIndex);

				var levelLights = FindObjectsOfType<Light>(playerIndex);
				foreach(Light light in levelLights) {
					if (light != blocksLight) {
						light.cullingMask &= light.GetComponentInParent<Visuals.Effects.FairyMatchingController>()
							? ~0 // Fairy lights up everything
							: ~GameLayers.BlocksMaskAll() // Light environment without the blocks.
							;

						// Only one directional light allowed.
						if (playerIndex != 0 && light.type == LightType.Directional) {
							light.enabled = false;
						}
					}
				}
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