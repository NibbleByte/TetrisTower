using DevLocker.GFrame.Input;
using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum ObjectiveStatus
	{
		InProgress,
		Completed,
		Failed,
	}

	/// <summary>
	/// Objectives base class. Based on the returned status player may win or lose the level.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public interface Objective
	{
		ObjectiveStatus Status { get; }

		void OnPostLevelLoaded(PlayerStatesContext context);
		void OnPreLevelUnloading();

		void Validate(UnityEngine.Object context);
	}

	public interface ObjectivesPresenter
	{
		string GreetingMessage { get; set; }

		void SetObjectiveText(Objective objective, string text);
		void ClearObjectiveText(Objective objective);

		bool HighlightBlock(GridCoords coords);
	}

	public class MultiObjectivesPresenter : ObjectivesPresenter
	{
		public readonly List<ObjectivesPresenter> Presenters = new List<ObjectivesPresenter>();

		public MultiObjectivesPresenter() { }

		public MultiObjectivesPresenter(params ObjectivesPresenter[] presenters) => Presenters.AddRange(presenters);

		public MultiObjectivesPresenter(IEnumerable<ObjectivesPresenter> presenters) => Presenters.AddRange(presenters);

		public string GreetingMessage {
			get => Presenters.FirstOrDefault()?.GreetingMessage;	// All presenters should have the same message.
			set {
				foreach(var presenter in Presenters) {
					presenter.GreetingMessage = value;
				}
			}
		}

		public void SetObjectiveText(Objective objective, string text)
		{
			foreach (var presenter in Presenters) {
				presenter.SetObjectiveText(objective, text);
			}
		}

		public void ClearObjectiveText(Objective objective)
		{
			foreach (var presenter in Presenters) {
				presenter.ClearObjectiveText(objective);
			}
		}

		public bool HighlightBlock(GridCoords coords)
		{
			bool success = false;

			foreach (var presenter in Presenters) {
				success = presenter.HighlightBlock(coords) || success;
			}

			return success;
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(Objective), true)]
	public class ObjectiveDrawer : SerializeReferenceCreatorDrawer<Objective>
	{
	}
#endif

}