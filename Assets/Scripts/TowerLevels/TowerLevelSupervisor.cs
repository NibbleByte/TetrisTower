using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
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

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "GameScene") {
				yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
			}

			var levelController = GameObject.FindObjectOfType<TowerLevelController>();
			levelController.Init(GameContext.CurrentPlaythrough.TowerLevel);

			var uiController = GameObject.FindObjectOfType<TowerLevelUIController>(true);

			StatesStack = new LevelStateStack(
				GameContext.GameConfig,
				GameContext.PlayerControls,
				GameContext.Options,
				levelController,
				uiController
				);

			yield return StatesStack.SetStateCrt(new TowerPlayState());

			// If save came with available matches, or pending actions, do them.
			var pendingActions = Logic.GameGridEvaluation.Evaluate(GameContext.CurrentPlaythrough.TowerLevel.Grid, GameContext.CurrentPlaythrough.TowerLevel.Rules);
			if (pendingActions.Count > 0) {
				yield return levelController.RunActions(pendingActions);
			}
		}

		public IEnumerator Unload()
		{
			yield break;
		}
	}
}