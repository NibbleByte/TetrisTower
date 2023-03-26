using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Supervisor to load the UITestScene used for testing out the UI + Input features of the GFrame.
	/// </summary>
	public class SampleUITesterLevelSupervisor : ILevelSupervisor
	{
		private InputEnabler m_InputEnabler;

#if GFRAME_ASYNC
		public async Task LoadAsync()
#else
		public IEnumerator Load()
#endif
		{
			SampleGameContext gameContext = SampleLevelsManager.Instance.GameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

#if UNITY_EDITOR
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-UITestScene") {
				// To bypass build settings list.
				var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None };
#if GFRAME_ASYNC
				var loadOp = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-UITestScene.unity", sceneParam);
				while (!loadOp.isDone) await Task.Yield();
#else
				yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Packages/devlocker.gframe/SampleGame/Scenes/Sample-UITestScene.unity", sceneParam);
#endif
			}
#else
			// Can pass it on as a parameter to the supervisor, instead of hard-coding it here.
			if (SceneManager.GetActiveScene().name != "Sample-UITestScene") {
#if GFRAME_ASYNC
				var loadOp = SceneManager.LoadSceneAsync("Sample-UITestScene", LoadSceneMode.Single);
				while (!loadOp.isDone) await Task.Yield();
#else
				yield return SceneManager.LoadSceneAsync("Sample-UITestScene", LoadSceneMode.Single);
#endif
			}
#endif

			// StateStack not needed for now.
			//var levelController = GameObject.FindObjectOfType<SampleMainMenuController>();

			PlayerContextUIRootObject.GlobalPlayerContext.CreatePlayerStack(
				gameContext.PlayerControls
				);

			// The whole level is UI, so enable it for the whole level.
			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(gameContext.PlayerControls.UI);
		}

#if GFRAME_ASYNC
		public Task UnloadAsync()
#else
		public IEnumerator Unload()
#endif
		{
			m_InputEnabler.Dispose();

#if GFRAME_ASYNC
			return Task.CompletedTask;
#else
			yield break;
#endif
		}
	}
}