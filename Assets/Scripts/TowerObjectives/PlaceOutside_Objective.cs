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

		public void Init(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelController);
			m_LevelController.PlacedOutsideGrid += OnPlacedOutsideGrid;
		}

		public void Deinit()
		{
			m_LevelController.PlacedOutsideGrid -= OnPlacedOutsideGrid;
		}

		private void OnPlacedOutsideGrid()
		{
			Status = PlacingOutsideIsWin ? ObjectiveStatus.Completed : ObjectiveStatus.Failed;
		}

		public string GetDisplayText()
		{
			if (PlacingOutsideIsWin)
				return "<color=\"orange\"><b>Endless!</b></color>";

			return string.Empty;
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}