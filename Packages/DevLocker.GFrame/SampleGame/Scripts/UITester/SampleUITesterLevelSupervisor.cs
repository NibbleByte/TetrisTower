using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Supervisor to load the UITestScene used for testing out the UI + Input features of the GFrame.
	/// </summary>
	public class SampleUITesterLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public SampleGameContext GameContext { get; private set; }

		public IEnumerator Load(IGameContext gameContext)
		{
			GameContext = (SampleGameContext)gameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

#if UNITY_EDITOR
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-UITestScene") {
				// To bypass build settings list.
				var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.Physics3D };
				yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-UITestScene.unity", sceneParam);
			}
#else
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-UITestScene") {
				yield return SceneManager.LoadSceneAsync("Sample-UITestScene", LoadSceneMode.Single);
			}
#endif

			// StateStack not needed for now.
			//var levelController = GameObject.FindObjectOfType<SampleMainMenuController>();
			//var levelController = GameObject.FindObjectOfType<SampleMainMenuController>();
			//
			//StatesStack = new LevelStateStack(
			//	GameContext.PlayerControls,
			//	levelController
			//	);

			// The whole level is UI, so enable it for the whole level.
			GameContext.PlayerControls.InputStack.PushActionsState(this);
			GameContext.PlayerControls.UI.Enable();
		}

		public IEnumerator Unload()
		{
			GameContext.PlayerControls.InputStack.PopActionsState(this);

			yield break;
		}
	}
}