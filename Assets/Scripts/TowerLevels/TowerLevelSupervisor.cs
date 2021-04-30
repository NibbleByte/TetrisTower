using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor, IGameContextProvider
	{
		public LevelStateStack StatesStack { get; private set; }

		public GameContext GameContext { get; private set; }

		public TowerLevelSupervisor(GameContext gameContext)
		{
			GameContext = gameContext;
		}

		public IEnumerator Load()
		{
			if (SceneManager.GetActiveScene().name != "GameScene") {
				yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
			}

			var levelController = GameObject.FindObjectOfType<TowerLevelController>();
			levelController.Init(GameContext.CurrentPlaythrough.TowerLevel);

			var uiController = GameObject.FindObjectOfType<TowerLevelUIController>(true);
			uiController.SetPause(false);

			StatesStack = new LevelStateStack(
				GameContext.GameConfig,
				GameContext.PlayerControls,
				levelController,
				uiController
				);

			StatesStack.SetState(new TowerPlayState());
		}

		public IEnumerator Unload()
		{
			yield break;
		}
	}
}