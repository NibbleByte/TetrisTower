using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	/// <summary>
	/// Supervisor to load the main menu and pass on the control.
	/// </summary>
	public class SampleMainMenuLevelSupervisor : ILevelSupervisor
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
			if (SceneManager.GetActiveScene().name != "Sample-MainMenuScene") {
				// To bypass build settings list.
				var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D };
				yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-MainMenuScene.unity", sceneParam);
			}
#else
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-MainMenuScene") {
				yield return SceneManager.LoadSceneAsync("Sample-MainMenuScene", LoadSceneMode.Single);
			}
#endif

			// StateStack not needed for now.
			//var levelController = GameObject.FindObjectOfType<SampleMainMenuController>();
			//
			//StatesStack = new LevelStateStack(
			//	GameContext.PlayerControls,
			//	levelController
			//	);

			// The whole level is UI, so enable it for the whole level.
			gameContext.PlayerControls.InputStack.PushActionsState(this);
			gameContext.PlayerControls.UI.Enable();
		}

		public IEnumerator Unload()
		{
			SampleLevelsManager.Instance.GameContext.PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}