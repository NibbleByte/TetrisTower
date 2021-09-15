using UnityEngine;
using UnityEngine.UI;

namespace DevLocker.GFrame.UIUtils
{
	/// <summary>
	/// Print the current supervisor and state in the UI.
	/// </summary>
	public class UIDebugShowSupervisorAndState : MonoBehaviour
	{
		public string SupervisorPrefix = "Supervisor: ";
		public Text SupervisorText;
		public string StatePrefix = "State: ";
		public Text StateText;

		private ILevelSupervisor m_CurrentLevelSupervisor;
		private ILevelState m_CurrentLevelState;

		private LevelsManager m_LevelsManager;

		void Awake()
		{
			if (SupervisorText) SupervisorText.text = string.Empty;
			if (StateText) StateText.text = string.Empty;
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
			ILevelState nextState = nextSupervisor?.StatesStack?.CurrentState;

			if (SupervisorText && nextSupervisor != m_CurrentLevelSupervisor) {
				m_CurrentLevelSupervisor = nextSupervisor;
				SupervisorText.text = SupervisorPrefix + m_CurrentLevelSupervisor?.GetType().Name ?? string.Empty;
			}

			if (StateText && nextState != m_CurrentLevelState) {
				m_CurrentLevelState = nextState;
				StateText.text = StatePrefix + m_CurrentLevelState?.GetType().Name ?? string.Empty;
			}
		}
	}
}