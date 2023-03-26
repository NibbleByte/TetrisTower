#if USE_TEXT_MESH_PRO
using DevLocker.GFrame.Input;
using TMPro;
using UnityEngine;


namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Print the current supervisor and state in the UI.
	/// </summary>
	public class UIDebugShowSupervisorAndState : MonoBehaviour
	{
		public string SupervisorPrefix = "Supervisor: ";
		public TextMeshProUGUI SupervisorText;

		public string StatePrefix = "State: ";
		public TextMeshProUGUI StateText;

		private ILevelSupervisor m_CurrentLevelSupervisor;
		private IPlayerState m_CurrentPlayerState;

		private LevelsManager m_LevelsManager;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		void Awake()
		{
			if (SupervisorText) SupervisorText.text = string.Empty;
			if (StateText) StateText.text = string.Empty;

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		void Update()
		{
			if (m_LevelsManager == null) {

				// Assuming there is only one instance.
				// Singleton (if present) is implemented by the user code which is not accessible here.
				m_LevelsManager = GameObject.FindObjectOfType<LevelsManager>();

				if (m_LevelsManager == null)
					return;
			}


			ILevelSupervisor nextSupervisor = m_LevelsManager.LevelSupervisor;
			IPlayerState nextState = m_PlayerContext.StatesStack?.CurrentState;

			if (SupervisorText && nextSupervisor != m_CurrentLevelSupervisor) {
				m_CurrentLevelSupervisor = nextSupervisor;
				SupervisorText.text = SupervisorPrefix + m_CurrentLevelSupervisor?.GetType().Name ?? string.Empty;
			}

			if (StateText && nextState != m_CurrentPlayerState) {
				m_CurrentPlayerState = nextState;
				StateText.text = StatePrefix + m_CurrentPlayerState?.GetType().Name ?? string.Empty;
			}
		}
	}
}
#endif