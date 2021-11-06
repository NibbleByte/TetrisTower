using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System;
using System.Collections;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public IEnumerator Load()
		{
			var gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "GameScene") {
				yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
			}

			var levelController = GameObject.FindObjectOfType<TowerLevelController>();
			levelController.Init(gameContext.CurrentPlaythrough.TowerLevel);

			var uiController = GameObject.FindObjectOfType<UI.TowerLevelUIController>(true);

			StatesStack = new LevelStateStack(
				gameContext.GameConfig,
				gameContext.PlayerControls,
				gameContext.Options,
				gameContext.CurrentPlaythrough,
				levelController,
				uiController
				);


			var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);

			foreach (var listener in behaviours.OfType<ILevelLoadingListener>()) {
				yield return listener.OnLevelLoading(StatesStack.ContextReferences);
			}

			foreach (var listener in behaviours.OfType<ILevelLoadedListener>()) {
				listener.OnLevelLoaded(StatesStack.ContextReferences);
			}

			yield return StatesStack.SetStateCrt(new TowerPlayState());

			// If save came with available matches, or pending actions, do them.
			var pendingActions = Logic.GameGridEvaluation.Evaluate(gameContext.CurrentPlaythrough.TowerLevel.Grid, gameContext.CurrentPlaythrough.TowerLevel.Rules);
			if (pendingActions.Count > 0) {
				yield return levelController.RunActions(pendingActions);
			}
		}

		public IEnumerator Unload()
		{
			var levelListeners = GameObject.FindObjectsOfType<MonoBehaviour>(true).OfType<ILevelLoadedListener>();
			foreach (var listener in levelListeners) {
				listener.OnLevelUnloading();
			}

			yield break;
		}
	}
}