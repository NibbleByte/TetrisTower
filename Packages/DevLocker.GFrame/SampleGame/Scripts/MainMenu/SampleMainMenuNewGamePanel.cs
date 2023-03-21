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
			m_PlayerContext.GetLevelManager().SwitchLevelAsync(new Play.SamplePlaySupervisor());
#else
			m_PlayerContext.GetLevelManager().SwitchLevel(new Play.SamplePlaySupervisor());
#endif
		}
	}

}