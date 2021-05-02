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
			var gameContext = (GameContext) LevelSupervisorsManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(gameContext.GameConfig.NewGameData);

			LevelSupervisorsManager.Instance.SwitchLevel(new TowerLevels.TowerLevelSupervisor());
		}
	}

}