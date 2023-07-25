using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class ObjectivesUIController : MonoBehaviour, ILevelLoadedListener
	{
		private class ObjectiveTextPair
		{
			public Objective Objective;
			public string Text;
		}

		public TextMeshProUGUI ObjectivesText;

		private List<ObjectiveTextPair> m_ObjectiveTexts = new List<ObjectiveTextPair>();

		public void OnLevelLoaded(PlayerStatesContext context)
		{
		}

		public void OnLevelUnloading()
		{
			m_ObjectiveTexts.Clear();
			if (ObjectivesText) {
				ObjectivesText.text = string.Empty;
			}
		}

		public void SetObjectiveText(Objective objective, string text)
		{
			text = text.Replace(@"\n", "\n").Trim();

			if (string.IsNullOrEmpty(text)) {
				ClearObjectiveText(objective);
				return;
			}

			ObjectiveTextPair pair = m_ObjectiveTexts.FirstOrDefault(pair => pair.Objective == objective);
			if (pair == null) {
				pair = new ObjectiveTextPair() { Objective = objective, Text = text };
				m_ObjectiveTexts.Add(pair);
			} else {
				pair.Text = text;
			}

			RefreshObjectiveTexts();
		}

		public void ClearObjectiveText(Objective objective)
		{
			int foundIndex = m_ObjectiveTexts.FindIndex(pair => pair.Objective == objective);
			if (foundIndex >= 0) {
				m_ObjectiveTexts.RemoveAt(foundIndex);
				RefreshObjectiveTexts();
			}
		}

		private void RefreshObjectiveTexts()
		{
			if (ObjectivesText) {
				ObjectivesText.text = string.Join("\n", m_ObjectiveTexts.Select(pair => pair.Text));
			}
		}
	}

}