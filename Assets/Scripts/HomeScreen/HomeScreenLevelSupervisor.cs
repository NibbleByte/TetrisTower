using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenLevelSupervisor : ILevelSupervisor, IGameContextProvider
	{
		public LevelStateStack StatesStack { get; private set; }

		public GameContext GameContext { get; private set; }

		public HomeScreenLevelSupervisor(GameContext gameContext)
		{
			GameContext = gameContext;
		}

		public IEnumerator Load()
		{
			yield return SceneManager.LoadSceneAsync("HomeScreenScene", LoadSceneMode.Single);

			StatesStack = new LevelStateStack(
				GameContext.GameConfig,
				GameContext.PlayerControls
				);
		}

		public IEnumerator Unload()
		{
			yield break;
		}
	}
}