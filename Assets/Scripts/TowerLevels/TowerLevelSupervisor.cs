using DevLocker.GameFrame;
using System;
using System.Collections;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public GameContext GameContext { get; private set; }

		public IEnumerator Load(IGameContext gameContext)
		{
			GameContext = (GameContext)gameContext;

			if (SceneManager.GetActiveScene().name != "GameScene") {
				yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
			}

			var levelController = GameObject.FindObjectOfType<TowerLevelController>();
			levelController.Init(GameContext.CurrentPlaythrough.TowerLevel);

			var uiController = GameObject.FindObjectOfType<TowerLevelUIController>(true);

			StatesStack = new LevelStateStack(
				GameContext.GameConfig,
				GameContext.PlayerControls,
				levelController,
				uiController
				);

			GameContext.PlayerControls.UI.Enable();
			GameContext.PlayerControls.CommonHotkeys.Enable();

			yield return StatesStack.SetStateCrt(new TowerPlayState());
		}

		public IEnumerator Unload()
		{
			GameContext.PlayerControls.UI.Disable();
			GameContext.PlayerControls.CommonHotkeys.Disable();

			yield break;
		}
	}
}