using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	/// <summary>
	/// Show greeting message while in Prepare state, i.e. waiting for player to start the level.
	/// </summary>
	public class GreetMessageUIController : MonoBehaviour, ILevelLoadedListener
	{
		private TextMeshProUGUI m_Text;

		private GridLevelController m_TowerLevel;

		void Awake()
		{
			m_Text = GetComponent<TextMeshProUGUI>();
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			if (m_Text == null) {
				m_Text = GetComponent<TextMeshProUGUI>();
			}

			context.SetByType(out m_TowerLevel);

			if (m_TowerLevel.LevelData.RunningState == TowerLevelRunningState.Preparing) {
				string message = m_TowerLevel.LevelData.GreetMessage.Replace(@"\n", "\n");

				foreach (Objective objective in m_TowerLevel.LevelData.Objectives) {
					message = objective.ProcessGreetMessage(message);
				}

				m_Text.text = message;

				enabled = !string.IsNullOrWhiteSpace(message);

			} else {
				m_Text.text = string.Empty;

				enabled = false;
			}
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel = null;
			m_Text.text = string.Empty;

			enabled = false;
		}

		void Update()
		{
			if (m_TowerLevel == null || m_TowerLevel.LevelData == null || m_TowerLevel.LevelData.RunningState != TowerLevelRunningState.Preparing) {
				m_Text.text = string.Empty;
				enabled = false;
				return;
			}
		}
	}
}