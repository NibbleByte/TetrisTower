using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
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
		public LevelStateStack StatesStack { get; private set; }

		public async Task LoadAsync()
		{
			var gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "HomeScreenScene") {
				var loadOp = SceneManager.LoadSceneAsync("HomeScreenScene", LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
			}

			TowerLevels.TowerLevelDebugAPI.__DebugInitialTowerLevel = string.Empty;

			// StateStack not needed for now.
			//var levelController = GameObject.FindObjectOfType<HomeScreenController>();
			//
			//StatesStack = new LevelStateStack(
			//	GameContext.GameConfig,
			//	GameContext.Options,
			//	GameContext.PlayerControls,
			//	levelController
			//	);

			// The whole level is UI, so enable it for the whole level.
			gameContext.PlayerControls.InputStack.PushActionsState(this);
			gameContext.PlayerControls.UI.Enable();
		}

		public Task UnloadAsync()
		{
			GameManager.Instance.GameContext.PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}
	}
}