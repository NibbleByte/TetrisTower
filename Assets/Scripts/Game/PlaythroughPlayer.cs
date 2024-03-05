using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace TetrisTower.Game
{
	/// <summary>
	/// Player instance with all its controllers used by the playthrough.
	/// </summary>
	public class PlaythroughPlayer
	{
		public bool IsPrimaryPlayer { get; private set; }
		public bool IsMultiplayer { get; private set; }
		public int PlayerIndex { get; private set; }
		public MultiplayerEventSystem EventSystem { get; private set; }
		public InputSystemUIInputModule InputModule { get; private set; }
		public PlayerControls PlayerControls { get; private set; }
		public PlayerContextUIRootObject PlayerContext { get; private set;}
		public Camera Camera { get; private set; }
		public Canvas[] UICanvases { get; private set; }

		public IInputContext InputContext { get; private set; }

		public GridLevelData LevelData => LevelController.LevelData;
		public GridLevelController LevelController { get; private set; }

		private PlaythroughPlayer() { }

		public static PlaythroughPlayer Create(
			GameConfig config,
			bool isMultiplayer,
			int playerIndex,
			GridLevelController levelController,
			Camera camera,
			PlayerContextUIRootObject playerContextRoot,
			Canvas[] uiCanvases
			)
		{
			var gameInputObject = GameObject.Instantiate(config.GameInputPrefab);
			SceneManager.MoveGameObjectToScene(gameInputObject, camera.gameObject.scene);

			var player = new PlaythroughPlayer {
				IsPrimaryPlayer = playerIndex == 0,
				PlayerIndex = playerIndex,
				IsMultiplayer = isMultiplayer,
				LevelController = levelController,

				PlayerControls = new PlayerControls(),
				PlayerContext = playerContextRoot,

				EventSystem = gameInputObject.GetComponentInChildren<MultiplayerEventSystem>(),
				InputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>(),

				Camera = camera,
			};

			player.InputModule.actionsAsset = player.PlayerControls.asset;  // This will refresh the UI Input action references to the new asset.

			player.InputContext = new InputCollectionContext(player.PlayerControls, player.PlayerControls.UI.Get(), config.BindingDisplayAssets);
			player.PlayerControls.SetInputContext(player.InputContext);

			// HACK: force the navigation to work, damn it! OnEnable() will enable the UI actions, so re-disable them again.
			player.EventSystem.gameObject.SetActive(false);
			player.EventSystem.gameObject.SetActive(true);
			player.PlayerControls.UI.Disable();

			player.PlayerContext.SetupPlayer(player.EventSystem, player.InputContext);

			player.UICanvases = uiCanvases;

			player.RenderCanvasesToCamera();

			return player;
		}

		public void Dispose()
		{
			PlayerContext.DisposePlayerStack();
			InputContext.Dispose();

			// Input & player context will be disposed by the level manager.
			GameObject.Destroy(EventSystem.gameObject);
			PlayerControls.Disable();
			PlayerControls.Dispose();
		}

		public void Pause(bool pauseInput, object source)
		{
			if (pauseInput) {
				InputContext.PushOrSetActionsMask(source, new InputAction[0]);
				HideCanvases();
			} else {
				RenderInputCanvasToScreen();
			}

			LevelController.PauseLevel(source);
		}

		public void Resume(object source)
		{
			RenderCanvasesToCamera();

			InputContext.PopActionsMask(source);
			LevelController.ResumeLevel(source);
		}

		public void RenderInputCanvasToScreen()
		{
			foreach (Canvas canvas in UICanvases) {
				canvas.enabled = canvas.gameObject == EventSystem.playerRoot;
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;

				// HACK: setting back RenderMode.ScreenSpaceOverlay breaks the sorting order,
				//		 making the loading screen appear behind the menus. Touch any sorting order to fix.
				canvas.sortingOrder++;
				canvas.sortingOrder--;
			}
		}

		public void RenderCanvasesToCamera()
		{
			// Skip to avoid performance slow down for single player
			// (while camera moves, canvas layout rebuilds as the rect "changes" because of camera transform)
			if (!IsMultiplayer)
				return;

			foreach (Canvas canvas in UICanvases) {
				canvas.enabled = true;
				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = Camera;
			}
		}

		public void HideCanvases()
		{
			foreach (Canvas canvas in UICanvases) {
				canvas.enabled = false;
			}
		}
	}
}