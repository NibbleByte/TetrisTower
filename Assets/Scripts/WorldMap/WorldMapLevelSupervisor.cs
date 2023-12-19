using DevLocker.GFrame;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.MessageBox;
using DevLocker.Utils;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.WorldMap
{
	public class WorldMapLevelSupervisor : ILevelSupervisor
	{
		private WorldPlaythroughData m_PlaythroughData;

		public WorldMapLevelSupervisor(WorldPlaythroughData playthroughData)
		{
			m_PlaythroughData = playthroughData;
		}

		public async Task LoadAsync()
		{
			GameContext gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (m_PlaythroughData.ActiveTowerLevels.Any()) {
				CriticalError($"Starting {nameof(WorldMapLevelSupervisor)} with tower level {m_PlaythroughData.ActiveTowerLevels[0].BackgroundScene} in progress. This is not allowed.", true);
				return;
			}

			SceneReference scene = Platforms.PlatformsUtils.IsMobileOrSimulator
				? gameContext.GameConfig.WorldMapSceneMobile
				: gameContext.GameConfig.WorldMapScene
				;

			if (SceneManager.GetActiveScene().name != scene.SceneName) {
				var loadOp = SceneManager.LoadSceneAsync(scene.ScenePath, LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}


			var levelController = GameObject.FindObjectOfType<WorldMapController>();

			levelController.Init(m_PlaythroughData);

			var uiController = GameObject.FindObjectOfType<UI.WorldMapUIController>(true);

			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			PlayerContextUIRootObject.GlobalPlayerContext.CreatePlayerStack(
				gameContext,
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				m_PlaythroughData,
				levelController,
				uiController
				);


			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				await listener.OnLevelLoadingAsync(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.Context);
			}

			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.SetState(new WorldMapPlayState());
		}

		public Task UnloadAsync()
		{
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

		private void CriticalError(string errorMessage, bool fallbackToHomescreen)
		{
			MessageBox.Instance.ShowSimple(
				"World Error",
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