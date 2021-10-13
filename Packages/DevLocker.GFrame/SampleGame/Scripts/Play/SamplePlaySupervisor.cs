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

		public IEnumerator Load()
		{
			SampleGameContext gameContext = SampleLevelsManager.Instance.GameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

#if UNITY_EDITOR
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-PlayScene") {
				// To bypass build settings list.
				var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None };
				yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-PlayScene.unity", sceneParam);
			}
#else
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-PlayScene") {
				yield return SceneManager.LoadSceneAsync("Sample-PlayScene", LoadSceneMode.Single);
			}
#endif

			var playerController = GameObject.FindObjectOfType<SamplePlayerController>();

			var uiController = GameObject.FindObjectOfType<SamplePlayUIController>(true);

			StatesStack = new LevelStateStack(
				gameContext.PlayerControls,
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