using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.MessageBox;
using DevLocker.Utils;
using System;
using System.Collections;
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

			// StateStack not needed for now.
			//var levelController = GameObject.FindObjectOfType<HomeScreenController>();
			//
			//StatesStack = PlayerContextUIRootObject.GlobalPlayerContext.CreatePlayerStack(
			//	GameContext.GameConfig,
			//	GameContext.Options,
			//	GameContext.PlayerControls,
			//	levelController
			//	);

			// The whole level is UI, so enable it for the whole level.
			gameContext.PlayerControls.Enable(this, gameContext.PlayerControls.UI);
		}

		public Task UnloadAsync()
		{
			GameManager.Instance.GameContext.PlayerControls.DisableAll(this);

			return Task.CompletedTask;
		}
	}
}