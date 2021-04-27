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
		public GameContext GameContext { get; private set; }

		public HomeScreenLevelSupervisor(GameContext gameContext)
		{
			GameContext = gameContext;
		}

		public IEnumerator Load()
		{
			yield return SceneManager.LoadSceneAsync("HomeScreenScene", LoadSceneMode.Single);

			var homeScreenLevel = GameObject.FindGameObjectWithTag("HomeScreenLevel");
			if (homeScreenLevel == null) {
				throw new Exception("Couldn't find level in the scene.");
			}

			LevelSupervisorComponent.AttachTo(homeScreenLevel, this);
		}

		public IEnumerator Unload()
		{
			yield break;
		}
	}
}