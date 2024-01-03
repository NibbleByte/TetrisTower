using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.MessageBox;
using DevLocker.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenLevelSupervisor : ILevelSupervisor
	{
		public async Task LoadAsync()
		{
			var gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			SceneReference scene = Platforms.PlatformsUtils.IsMobileOrSimulator
				? gameContext.GameConfig.BootSceneMobile
				: gameContext.GameConfig.BootScene
				;

			if (SceneManager.GetActiveScene().name != scene.SceneName) {
				var loadOp = SceneManager.LoadSceneAsync(scene.SceneName, LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}

			TowerLevels.TowerLevelDebugAPI.__DebugInitialTowerLevel = string.Empty;

			gameContext.SetCurrentPlaythrough((IPlaythroughData) null);

			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			// StateStack not needed for now.
			var levelController = behaviours.OfType<HomeScreenController>().First();

			PlayerContextUIRootObject.GlobalPlayerContext.CreatePlayerStack(
				gameContext,
				gameContext.GameConfig,
				gameContext.UserPrefs,
				gameContext.GlobalControls,
				levelController
				);

			// The whole level is UI, so enable it for the whole level.
			gameContext.GlobalControls.Enable(this, gameContext.GlobalControls.UI);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}
		}

		public Task UnloadAsync()
		{
			GameManager.Instance.GameContext.GlobalControls.DisableAll(this);

			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

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

			return Task.CompletedTask;
		}
	}
}