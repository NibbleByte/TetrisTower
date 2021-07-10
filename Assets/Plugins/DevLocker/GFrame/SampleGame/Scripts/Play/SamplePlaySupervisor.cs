using DevLocker.GFrame.SampleGame.Game;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.Play
{
	public class SamplePlaySupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public SampleGameContext GameContext { get; private set; }

		public IEnumerator Load(IGameContext gameContext)
		{
			GameContext = (SampleGameContext)gameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "Sample-PlayScene") {
				yield return SceneManager.LoadSceneAsync("Sample-PlayScene", LoadSceneMode.Single);
			}

			var playerController = GameObject.FindObjectOfType<SamplePlayerController>();

			var uiController = GameObject.FindObjectOfType<SamplePlayUIController>(true);

			StatesStack = new LevelStateStack(
				GameContext.PlayerControls,
				playerController,
				uiController
				);

			yield return StatesStack.SetStateCrt(new SamplePlayJumperState());
		}

		public IEnumerator Unload()
		{
			yield break;
		}
	}
}