using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Linq;
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
		public string Message {
			get => m_Text.text;
			set {
				m_Text.text = value.Replace(@"\n", "\n");

				enabled = m_TowerLevel != null
					&& m_TowerLevel.LevelData.RunningState == TowerLevelRunningState.Preparing
					&& !string.IsNullOrWhiteSpace(value)
					;
			}
		}

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

			Message = m_TowerLevel.LevelData.GreetMessage.Replace(@"\n", "\n");
		}

		public void OnLevelUnloading()
		{
			Message = string.Empty;
			m_TowerLevel = null;
		}

		void Update()
		{
			if (m_TowerLevel == null || m_TowerLevel.LevelData == null || m_TowerLevel.LevelData.RunningState != TowerLevelRunningState.Preparing) {
				Message = string.Empty;
				return;
			}
		}
	}
}