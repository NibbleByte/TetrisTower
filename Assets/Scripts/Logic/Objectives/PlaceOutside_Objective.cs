using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace TetrisTower.Logic.Objectives
{
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