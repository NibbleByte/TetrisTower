using DevLocker.GFrame.SampleGame.Game;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.Play
{
	/// <summary>
	/// Supervisor to load the sample play scene used to demonstrate sample gameplay with the GFrame,
	/// focusing on play states & input hotkeys.
	/// </summary>
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

			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
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