using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System;
using System.Collections;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public IEnumerator Load()
		{
			var gameContext = GameManager.Instance.GameContext;

			if (MessageBox.Instance) {
				MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "HomeScreenScene") {
				yield return SceneManager.LoadSceneAsync("HomeScreenScene", LoadSceneMode.Single);
			}

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

		public IEnumerator Unload()
		{
			GameManager.Instance.GameContext.PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}