using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.SampleGame.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Similarly to <see cref="SamplePlaySupervisor"/>, supervisor to load the sample play scene used to demonstrate sample gameplay with the GFrame,
	/// focusing on play states & input hotkeys.
	/// This one supports multiple players.
	///
	/// For more info check this video: https://www.youtube.com/watch?v=g_s0y5yFxYg
	/// </summary>
	public class SampleMultiPlaySupervisor : ILevelSupervisor
	{

#if GFRAME_ASYNC
		public async Task LoadAsync()
#else
		public IEnumerator Load()
#endif
		{
			SampleGameContext gameContext = SampleLevelsManager.Instance.GameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

			// Disable the global PlayerInput component as each player will have their own.
			gameContext.PlayerInput.gameObject.SetActive(false);

#if UNITY_EDITOR
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-MultiPlayScene") {
				// To bypass build settings list.
				var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None };
#if GFRAME_ASYNC
				var loadOp = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-MultiPlayScene.unity", sceneParam);
				while (!loadOp.isDone) await Task.Yield();
#else
				yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-MultiPlayScene.unity", sceneParam);
#endif
			}
#else
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-PlayScene") {
#if GFRAME_ASYNC
				var loadOp = SceneManager.LoadSceneAsync("Sample-MultiPlayScene", LoadSceneMode.Single);
				while(!loadOp.isDone) await Task.Yield();
#else
				yield return SceneManager.LoadSceneAsync("Sample-MultiPlayScene", LoadSceneMode.Single);
#endif
			}
#endif

			var eventSystems = GameObject.FindObjectsByType<MultiplayerEventSystem>(FindObjectsSortMode.None);
			Array.Sort(eventSystems, (left, right) => left.name.CompareTo(right.name));

			// Setup all the context and stacks for each player.

			foreach(MultiplayerEventSystem eventSystem in eventSystems) {
				var playerControls = new SamplePlayerControls();

				var playerInput = eventSystem.GetComponent<PlayerInput>();
				var playerController = playerInput.camera.GetComponentInParent<SamplePlayerController>();
				var uiController = eventSystem.playerRoot.GetComponentInParent<SamplePlayUIController>();

				var playerContext = uiController.GetComponent<PlayerContextUIRootObject>();

				playerInput.actions = playerControls.asset;

				var uiInputModule = eventSystem.GetComponentInChildren<InputSystemUIInputModule>();
				uiInputModule.actionsAsset = playerControls.asset;  // This will refresh the UI Input action references to the new asset.

				playerInput.uiInputModule = uiInputModule;

				// HACK: Starting two PlayerInput components while disabling the global one in the same startup frame doesn't work - no input is called.
				//		 Force them refresh using crude methods. This probably won't be needed if instantiating players dynamically.
				eventSystem.gameObject.SetActive(false);
				eventSystem.gameObject.SetActive(true);

				var inputContext = new InputComponentContext(playerInput, new InputActionsMaskedStack(playerControls), GameObject.FindObjectOfType<SampleGameStarter>().BindingDisplayAssets);
				playerControls.SetInputContext(inputContext);

				//
				// Now the states stack & UI root...
				//
				playerContext.SetupPlayer(eventSystem, inputContext);

				playerContext.CreatePlayerStack(
					playerControls,
					playerController,
					uiController
				);


				// Only collect behaviours for this player and notify them with the correct references.
				var behaviours = CollectBehaviours(playerController, uiController, eventSystem);

				foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
#if GFRAME_ASYNC
					await listener.OnLevelLoadingAsync(playerContext.StatesStack.Context);
#else
					yield return listener.OnLevelLoading(playerContext.StatesStack.Context);
#endif
				}

				foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
					listener.OnLevelLoaded(playerContext.StatesStack.Context);
				}


				playerContext.StatesStack.SetState(new SamplePlayJumperState());
			}
		}


#if GFRAME_ASYNC
		public Task UnloadAsync()
#else
		public IEnumerator Unload()
#endif
		{
			// Stop the manager as it will log errors complaining about the global PlayerInput not having camera.
			GameObject.FindObjectOfType<PlayerInputManager>().enabled = false;

			// Same goes for player input & event systems. Note: collection is modified on disable PlayerInput
			foreach(PlayerInput playerInput in PlayerInput.all.ToList()) {
				if (playerInput) {
					playerInput.gameObject.SetActive(false);
				}
			}

			var levelListeners = GameObject.FindObjectsOfType<MonoBehaviour>(true).OfType<ILevelLoadedListener>();
			foreach (var listener in levelListeners) {
				listener.OnLevelUnloading();
			}

			SampleLevelsManager.Instance.GameContext.PlayerInput.gameObject.SetActive(true);

			// Enabling input components causes for UI InputActions to get enabled, which confuses the MainMenu supervisor, who also enables them.
			SampleLevelsManager.Instance.GameContext.PlayerControls.Sample_UI.Disable();

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
		}


		private static IEnumerable<MonoBehaviour> CollectBehaviours(params MonoBehaviour[] behaviours)
		{
			foreach(MonoBehaviour behaviour in behaviours) {
				foreach(var collectedBehaviour in behaviour.GetComponentsInChildren<MonoBehaviour>(true)) {
					yield return collectedBehaviour;
				}
			}
		}
	}
}