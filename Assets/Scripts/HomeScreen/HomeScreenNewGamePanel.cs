using System;
using System.Collections.Generic;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			var supervisorComponent = GetComponentInParent<LevelSupervisorComponent>();
			var gameContext = supervisorComponent.GetGameContext();

			gameContext.SetCurrentPlaythrough(gameContext.GameConfig.NewGameData);

			supervisorComponent.SwitchLevel(new TowerLevels.TowerLevelSupervisor(gameContext));
		}
	}

}