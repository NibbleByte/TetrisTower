using DevLocker.Utils;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TetrisTower.GameStarter
{
	public class DisclaimerScreenController : MonoBehaviour
	{
		public GameConfig GameConfig;

		private void Update()
		{
			if (Gamepad.current != null) {
				if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame) {
					LoadHomeScreenScene();
				}
			}

			if (Keyboard.current != null) {
				if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) {
					LoadHomeScreenScene();
				}
			}
		}

		private void LoadHomeScreenScene()
		{
			SceneReference scene = Platforms.PlatformsUtils.IsMobileOrSimulator
				? GameConfig.HomeScreenSceneMobile
				: GameConfig.HomeScreenScene
				;

#if UNITY_EDITOR
			var sceneParam = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None };
			UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(scene.ScenePath, sceneParam);
#else
			SceneManager.LoadSceneAsync(scene.SceneName, LoadSceneMode.Single);
#endif
		}
	}
}