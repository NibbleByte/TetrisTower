using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Logic.Objectives
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

		public void Init(GridLevelController levelController)
		{
			levelController.PlacedOutsideGrid += OnPlacedOutsideGrid;
		}

		public void Deinit(GridLevelController levelController)
		{
			levelController.PlacedOutsideGrid -= OnPlacedOutsideGrid;
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

		public string ProcessGreetMessage(string message)
		{
			if (PlacingOutsideIsWin && string.IsNullOrWhiteSpace(message))
				return "Endless!";

			return message;
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}