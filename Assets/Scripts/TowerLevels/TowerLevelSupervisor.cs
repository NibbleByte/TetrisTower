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

		public TowerLevelSupervisor(IPlaythroughData playthroughData)
		{
			m_PlaythroughData = playthroughData;
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

			if (m_PlaythroughData.TowerLevel == null) {

				m_PlaythroughData.SetupCurrentTowerLevel(gameContext.GameConfig, m_OverrideScene);

				if (m_PlaythroughData.TowerLevel == null) {
					CriticalError($"No available level.", true);
					return;
				}

				if (m_PlaythroughData.TowerLevel.BackgroundScene.IsEmpty) {
					Debug.LogError($"No appropriate scene found in current level! Setting dev one.");
					m_PlaythroughData.TowerLevel.BackgroundScene = new SceneReference("Assets/Scenes/_DevTowerScene.unity");

					bool isValidFallback = SceneUtility.GetBuildIndexByScenePath(m_PlaythroughData.TowerLevel.BackgroundScene.ScenePath) >= 0;

					CriticalError($"Current level did not have scene specified. Loading fallback.", !isValidFallback);

					if (!isValidFallback) {
						return;
					}
				}

			} else if (m_PlaythroughData.TowerLevel.BlocksSkinStack == null || m_PlaythroughData.TowerLevel.BlocksSkinStack.IsEmpty) {

				// For debug saves, blocks may be missing. Fill them up with the defaults.
				m_PlaythroughData.TowerLevel.BlocksSkinStack = new BlocksSkinStack(gameContext.GameConfig.DefaultBlocksSet, m_PlaythroughData.BlocksSet);
				m_PlaythroughData.TowerLevel.BlocksSkinStack.Validate(gameContext.GameConfig.AssetsRepository, gameContext.GameConfig);
			}

			var backgroundScene = m_PlaythroughData.TowerLevel.BackgroundScene;
			if (SceneManager.GetActiveScene().name != backgroundScene.SceneName) {
				var loadOp = SceneManager.LoadSceneAsync(backgroundScene.ScenePath, LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}

			List<Transform> restPoints;
			Visuals.Effects.FairyMatchingController fairy;

			var levelController = GameObject.FindObjectOfType<GridLevelController>();
			if (levelController == null) {
				var placeholder = GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag);
				if (placeholder == null) {
					throw new Exception($"Scene {SceneManager.GetActiveScene().name} has missing level controller and placeholder. Cannot load current level.");
				}

				levelController = GameObject.Instantiate<GridLevelController>(gameContext.GameConfig.TowerLevelController, placeholder.transform.position, placeholder.transform.rotation);

				Light overrideBlocksLight = placeholder.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));
				if (overrideBlocksLight) {
					Light blocksLight = levelController.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));
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
			}

			SetupLights(levelController);

			var uiController = GameObject.FindObjectOfType<TowerLevelUIController>(true);
			if (uiController == null) {
				GameObject[] uiPrefabs = Platforms.PlatformsUtils.IsMobileOrSimulator
					? gameContext.GameConfig.UIPrefabsMobile
					: gameContext.GameConfig.UIPrefabs
					;

				foreach (GameObject prefab in uiPrefabs) {
					var instance = GameObject.Instantiate<GameObject>(prefab);
					instance.name = prefab.name;

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
				int seed = m_PlaythroughData.TowerLevel.RandomInitialLevelSeed;
				visualsRandom = new Xoshiro.PRNG32.XoShiRo128starstar(seed);

				recordComponent = levelController.gameObject.AddComponent<LevelReplayRecorder>();
				recordComponent.Recording.SaveInitialState(m_PlaythroughData.TowerLevel, gameContext.GameConfig, fairy, restPoints, seed);
				recordComponent.Recording.GridLevelController = levelController;
				recordComponent.Recording.Fairy = fairy;
				recordComponent.enabled = false;

				timing = recordComponent.Timing;
			}

			uiController.SetIsReplayPlaying(playbackComponent != null);

			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			PlayerContextUIRootObject.GlobalPlayerContext.CreatePlayerStack(
				gameContext,
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				m_PlaythroughData,
				m_PlaythroughData.TowerLevel,
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
			levelController.Init(m_PlaythroughData.TowerLevel, timing);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}

			// Other visuals depend on this, so init it first.
			behaviours.OfType<TowerConeVisualsController>().First().Init(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context, visualsRandom);

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}

			foreach(var objective in levelController.LevelData.Objectives) {
				objective.OnPostLevelLoaded(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}


			if (playbackComponent) {
				playbackComponent.enabled = true;
			} else {
				recordComponent.enabled = true;
			}

			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.SetState(playbackComponent ? new TowerReplayPlaybackState() : new TowerPlayState());

			if (m_PlaythroughData.TowerLevel.IsPlaying) {
				GridLevelData levelData = m_PlaythroughData.TowerLevel;
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
			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

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

			return Task.CompletedTask;
		}

		private static void SetupLights(GridLevelController levelController)
		{
			var blocksLight = levelController.GetComponentsInChildren<Light>().FirstOrDefault(l => l.CompareTag(GameTags.BlocksLight));

			if (blocksLight) {
				blocksLight.cullingMask = GameLayers.BlocksMask;

				var levelLights = GameObject.FindObjectsOfType<Light>();
				foreach(Light light in levelLights) {
					if (light != blocksLight) {
						light.cullingMask &= light.GetComponentInParent<Visuals.Effects.FairyMatchingController>()
							? ~0 // Fairy lights up everything
							: ~GameLayers.BlocksMask // Light environment without the blocks.
							;
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