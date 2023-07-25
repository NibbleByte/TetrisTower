using DevLocker.GFrame.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using UnityEngine;

namespace TetrisTower.TowerObjectives
{
	/// <summary>
	/// Objective is completed when specified <see cref="Coordinates"/> are cleared.
	/// Cannot fail.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class ClearCoordinates_Objective : Objective
	{
		public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.InProgress;

		public List<GridCoords> Coordinates = new List<GridCoords>();

		[JsonIgnore]
		private ConeVisualsGrid m_VisualsGrid;

		[JsonIgnore]
		private ScoreGrid m_ScoreGrid;

		[JsonIgnore]
		private TowerUI.ObjectivesUIController m_ObjectivesUIController;

		public void OnPostLevelLoaded(PlayerStatesContext context)
		{
			// TODO: Validate if blocks exist on the coordinates?
			context.SetByType(out m_VisualsGrid);

			m_ScoreGrid = context.FindByType<GridLevelController>().LevelData.Score;
			m_ScoreGrid.ClearActionScored += OnClearActionScored;

			m_ObjectivesUIController = context.TryFindByType<TowerUI.ObjectivesUIController>();
			TryDisplayObjective();
		}

		// For unit tests.
		public void OnPreLevelUnloading()
		{
			m_ScoreGrid.ClearActionScored -= OnClearActionScored;
		}

		private void OnClearActionScored(ClearMatchedAction action)
		{
			foreach(var coords in action.Coords) {
				Coordinates.Remove(coords);
			}

			if (Coordinates.Count == 0) {
				Status = ObjectiveStatus.Completed;
			}
		}

		private void TryDisplayObjective()
		{
			if (m_ObjectivesUIController == null)
				return;

			m_ObjectivesUIController.SetObjectiveText(this, $"Match the highlighted blocks");
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}