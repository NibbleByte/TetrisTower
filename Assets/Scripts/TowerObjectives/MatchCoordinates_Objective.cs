using DevLocker.GFrame.Input;
using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerObjectives
{
	/// <summary>
	/// Objective is completed when specified <see cref="Coordinates"/> are cleared.
	/// Cannot fail.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class MatchCoordinates_Objective : Objective
	{
		public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.InProgress;

		public List<GridCoords> Coordinates = new List<GridCoords>();

		[JsonIgnore]
		private ScoreGrid m_ScoreGrid;

		public void Init(PlayerStatesContext context)
		{
			// TODO: Validate if blocks exist on the coordinates?
			var levelController = context.FindByType<GridLevelController>();
			Init(levelController.LevelData.Score);
		}

		// For unit tests.
		public void Init(ScoreGrid score)
		{
			m_ScoreGrid = score;
			m_ScoreGrid.ClearActionScored += OnClearActionScored;
		}

		// For unit tests.
		public void Deinit()
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

		public string GetDisplayText()
		{
			return $"Match the highlighted blocks";
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}