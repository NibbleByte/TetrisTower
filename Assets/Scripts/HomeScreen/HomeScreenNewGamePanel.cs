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
			if (GameController.Instance) {
				GameController.Instance.StartNewGame(GameController.Instance.NewGameData);
			}
		}
	}

}