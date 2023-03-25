using DevLocker.GFrame.Input;
using DevLocker.GFrame.SampleGame.Game;
using UnityEngine;

namespace DevLocker.GFrame.SampleGame.MainMenu
{
	public class SampleMainMenuNewGamePanel : MonoBehaviour
	{
		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		private void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		public void StartNewGame()
		{
#if GFRAME_ASYNC
			Game.SampleLevelsManager.Instance.SwitchLevelAsync(new Play.SamplePlaySupervisor());
#else
			Game.SampleLevelsManager.Instance.SwitchLevel(new Play.SamplePlaySupervisor());
#endif
		}

		public void StartMultiplayerNewGame()
		{
#if GFRAME_ASYNC
			Game.SampleLevelsManager.Instance.SwitchLevelAsync(new Play.SampleMultiPlaySupervisor());
#else
			Game.SampleLevelsManager.Instance.SwitchLevel(new Play.SampleMultiPlaySupervisor());
#endif
		}
	}

}