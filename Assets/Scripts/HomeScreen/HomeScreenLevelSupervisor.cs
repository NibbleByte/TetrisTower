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

		public GameContext GameContext { get; private set; }

		public IEnumerator Load(IGameContext gameContext)
		{
			GameContext = (GameContext)gameContext;

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
			//	GameContext.PlayerControls,
			//	levelController
			//	);

			// The whole level is UI, so enable it for the whole level.
			GameContext.PlayerControls.InputStack.PushActionsState(this);
			GameContext.PlayerControls.UI.Enable();
			GameContext.PlayerControls.CommonHotkeys.Enable();
		}

		public IEnumerator Unload()
		{
			GameContext.PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}