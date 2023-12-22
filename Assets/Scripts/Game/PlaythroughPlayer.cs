using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace TetrisTower.Game
{
	/// <summary>
	/// Player instance with all its controllers used by the playthrough.
	/// </summary>
	public class PlaythroughPlayer
	{
		public MultiplayerEventSystem EventSystem { get; private set; }
		public InputSystemUIInputModule InputModule { get; private set; }
		public PlayerControls PlayerControls { get; private set; }
		public PlayerContextUIRootObject PlayerContext { get; private set;}
		//public PlayerInput PlayerInput { get; private set; }
		public Camera Camera { get; private set; }

		public IInputContext InputContext { get; private set; }

		public GridLevelController LevelController { get; private set; }


		public static PlaythroughPlayer Create(GameConfig config, GridLevelController levelController, Camera camera, GameObject uiRoot)
		{
			var gameInputObject = GameObject.Instantiate(config.GameInputPrefab);
			SceneManager.MoveGameObjectToScene(gameInputObject, camera.gameObject.scene);

			var player = new PlaythroughPlayer {
				LevelController = levelController,

				PlayerControls = new PlayerControls(),
				PlayerContext = uiRoot.GetComponentInChildren<PlayerContextUIRootObject>(),

				EventSystem = gameInputObject.GetComponentInChildren<MultiplayerEventSystem>(),
				InputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>(),
				//PlayerInput = gameInputObject.GetComponentInChildren<PlayerInput>(),

				Camera = camera,
			};

			//player.PlayerInput.actions = player.PlayerControls.asset;

			player.InputModule.actionsAsset = player.PlayerControls.asset;  // This will refresh the UI Input action references to the new asset.

			//player.PlayerInput.uiInputModule = player.InputModule;

			player.InputContext = new InputCollectionContext(player.PlayerControls, player.PlayerControls.UI.Get(), config.BindingDisplayAssets);
			player.PlayerControls.SetInputContext(player.InputContext);

			player.PlayerContext.SetupPlayer(player.EventSystem, player.InputContext);

			return player;
		}

		public void Pause(bool pauseInput)
		{
			// TODO: Pause Input
			LevelController.PauseLevel();
		}

		public void Resume()
		{
			// TODO: Resume Input
			LevelController.ResumeLevel();
		}

		public void Dispose()
		{
			// Input & player context will be disposed by the level manager.
			GameObject.Destroy(EventSystem.gameObject);
			PlayerControls.Dispose();
			PlayerControls.Disable();
		}
	}
}