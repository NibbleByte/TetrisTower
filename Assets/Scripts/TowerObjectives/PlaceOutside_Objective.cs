using DevLocker.GFrame.Input;
using Newtonsoft.Json;
using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerObjectives
{
	/// <summary>
	/// When blocks are placed outside the tower (at the top), this objective is triggered.
	/// It is considered complete or failed based on the <see cref="PlacingOutsideIsWin"/> flag.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PlaceOutside_Objective : Objective
	{
		public bool PlacingOutsideIsWin = false;

		public ObjectiveStatus Status { get; private set; }

		[JsonIgnore]
		private GridLevelController m_LevelController;

		[JsonIgnore]
		private TowerUI.ObjectivesUIController m_ObjectivesUIController;

		public void OnPostLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelController);
			m_LevelController.PlacedOutsideGrid += OnPlacedOutsideGrid;

			m_ObjectivesUIController = context.TryFindByType<TowerUI.ObjectivesUIController>();

			TryDisplayObjective();
		}

		public void OnPreLevelUnloading()
		{
			m_LevelController.PlacedOutsideGrid -= OnPlacedOutsideGrid;
		}

		private void OnPlacedOutsideGrid()
		{
			Status = PlacingOutsideIsWin ? ObjectiveStatus.Completed : ObjectiveStatus.Failed;
		}

		private void TryDisplayObjective()
		{
			if (m_ObjectivesUIController == null)
				return;

			if (PlacingOutsideIsWin) {
				m_ObjectivesUIController.SetObjectiveText(this, "<color=\"orange\"><b>Endless!</b></color>");
			}
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}