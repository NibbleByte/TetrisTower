using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class ObjectivesUIController : MonoBehaviour, ILevelLoadedListener, ObjectivesPresenter
	{
		private class ObjectiveTextPair
		{
			public Objective Objective;
			public string Text;
		}

		public TextMeshProUGUI ObjectivesText;

		private GreetMessageUIController m_GreetMessageController;

		private ConeVisualsGrid m_VisualsGrid;

		private List<ObjectiveTextPair> m_ObjectiveTexts = new List<ObjectiveTextPair>();

		public string GreetingMessage {
			get => m_GreetMessageController?.Message;
			set => m_GreetMessageController.Message = value;
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			// HACK: At the moment the greet message & visuals grid is shared between multiple objectives controllers. Operations are set twice.
			context.SetByType(out m_GreetMessageController);

			context.TrySetByType(out m_VisualsGrid);
		}

		public void OnLevelUnloading()
		{
			m_GreetMessageController = null;
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

		public bool HighlightAsObjective(GridCoords coords)
		{
			ConeVisualsBlock block = m_VisualsGrid[coords];
			if (block == null) {
				return false;
			}

			block.IsObjective = true;
			return true;
		}
	}

}