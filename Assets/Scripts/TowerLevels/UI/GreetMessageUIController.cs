using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerLevels.UI
{
	/// <summary>
	/// Show greeting message while in Prepare state, i.e. waiting for player to start the level.
	/// </summary>
	public class GreetMessageUIController : MonoBehaviour, ILevelLoadedListener
	{
		private TextMeshProUGUI m_Text;

		private GridLevelController m_TowerLevel;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			m_Text = GetComponent<TextMeshProUGUI>();
			context.SetByType(out m_TowerLevel);

			if (m_TowerLevel.LevelData.RunningState == TowerLevelRunningState.Preparing && !string.IsNullOrEmpty(m_TowerLevel.LevelData.GreetMessage)) {
				m_Text.text = m_TowerLevel.LevelData.GreetMessage
					.Replace(@"\n", "\n")
					.Replace(@"{ObjectiveEndCount}", m_TowerLevel.LevelData.ObjectiveEndCount.ToString());

				enabled = true;

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