using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerUI
{

	public class TowerUIDecorationsController : MonoBehaviour, ILevelLoadedListener
	{
		public GameObject SinglePlayerDecorations;
		public GameObject MultiPlayerDecorations;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			bool isSinglePlayer = context.FindByType<IPlaythroughData>().IsSinglePlayer;
			SinglePlayerDecorations.SetActive(isSinglePlayer);
			MultiPlayerDecorations.SetActive(!isSinglePlayer);
		}

		public void OnLevelUnloading()
		{

		}
	}
}