using DevLocker.GFrame.Input;
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
	public class ClearCoordinates_Objective : Objective
	{
		[field: JsonProperty(nameof(Status))]
		public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.InProgress;

		public List<GridCoords> Coordinates = new List<GridCoords>();

		[JsonIgnore]
		private BlocksGrid m_BlocksGrid;

		[JsonIgnore]
		private ScoreGrid m_ScoreGrid;

		[JsonIgnore]
		private ObjectivesPresenter m_Presenter;

		public void OnPostLevelLoaded(PlayerStatesContext context)
		{
			m_BlocksGrid = context.FindByType<GridLevelData>().Grid;
			m_BlocksGrid.BlockMoved += OnBlocksMoved;
			m_BlocksGrid.BlockDestroyed += OnBlocksDestroyed;

			m_ScoreGrid = context.FindByType<GridLevelData>().Score;
			m_ScoreGrid.ClearActionScored += OnClearActionScored;

			m_Presenter = context.TryFindByType<ObjectivesPresenter>();
			TryDisplayObjective();
			TryHighlightTargetBlocks();
		}

		// For unit tests.
		public void OnPreLevelUnloading()
		{
			m_BlocksGrid.BlockMoved -= OnBlocksMoved;
			m_BlocksGrid.BlockDestroyed -= OnBlocksDestroyed;
			m_ScoreGrid.ClearActionScored -= OnClearActionScored;
		}

		private void OnBlocksMoved(GridCoords from, GridCoords to)
		{
			for (int i = 0; i < Coordinates.Count; i++) {
				if (Coordinates[i] == from) {
					Coordinates[i] = to;
				}
			}
		}

		private void OnBlocksDestroyed(GridCoords coords)
		{
			if (Coordinates.Contains(coords)) {
				Debug.Log($"Objective at {coords} got destroyed - fail.");
				Status = ObjectiveStatus.Failed;
			}
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
			if (m_Presenter == null)
				return;

			m_Presenter.SetObjectiveText(this, $"Match the highlighted blocks");
		}

		private void TryHighlightTargetBlocks()
		{
			if (m_Presenter == null)
				return;

			for (int i = 0; i < Coordinates.Count; ++i) {
				GridCoords coords = Coordinates[i];

				bool success = m_Presenter.HighlightAsObjective(coords);
				if (!success) {
					Debug.LogWarning($"ClearCoordinates objective needs block at {coords}, but none was found. No block will be highlighted");
					Coordinates.RemoveAt(i);
					i--;
					continue;
				}
			}
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}