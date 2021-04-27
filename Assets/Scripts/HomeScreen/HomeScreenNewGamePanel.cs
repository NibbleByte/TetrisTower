using System;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenNewGamePanel : MonoBehaviour
	{
		public void StartNewGame()
		{
			var supervisor = GetComponentInParent<LevelSupervisorComponent>().LevelSupervisor;

			supervisor.GameContext.SetCurrentPlaythrough(supervisor.GameContext.GameConfig.NewGameData);
			supervisor.SwitchLevel(new TowerLevels.TowerLevelSupervisor(supervisor.GameContext));
		}
	}

}