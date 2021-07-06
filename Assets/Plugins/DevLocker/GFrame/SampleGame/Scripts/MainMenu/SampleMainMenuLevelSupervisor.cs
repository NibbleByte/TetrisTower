using DevLocker.GFrame.SampleGame.Game;
using System.Collections;
using UnityEngine.SceneManagement;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuLevelSupervisor : ILevelSupervisor
	{
		public LevelStateStack StatesStack { get; private set; }

		public SampleGameContext GameContext { get; private set; }

		public IEnumerator Load(IGameContext gameContext)
		{
			GameContext = (SampleGameContext)gameContext;

			if (MessageBox.MessageBox.Instance) {
				MessageBox.MessageBox.Instance.ForceCloseAllMessages();
			}

			if (SceneManager.GetActiveScene().name != "Sample-MainMenuScene") {
				yield return SceneManager.LoadSceneAsync("Sample-MainMenuScene", LoadSceneMode.Single);
			}

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