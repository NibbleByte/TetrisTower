using DevLocker.GFrame;
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
			var gameContext = (GameContext) LevelsManager.Instance.GameContext;

			gameContext.SetCurrentPlaythrough(gameContext.GameConfig.NewGameData);

			LevelsManager.Instance.SwitchLevel(new TowerLevels.TowerLevelSupervisor());
		}
	}

}