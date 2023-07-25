using DevLocker.GFrame.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerObjectives
{
	/// <summary>
	/// Waits for all objectives in the list to be completed before returning complete status itself.
	/// Any failed objective automatically fails this one as well if <see cref="AreOptional"/> is checked.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class List_Objective : Objective
	{
		// TODO: Maybe each objective should have "IsOptional" field instead.
		[Tooltip("Is player allowed to fail the objectives with no consequences. If true, this objective can't fail. If not, first failure from the list will fail this objective as well")]
		public bool AreOptional = false;

		[SerializeReference]
		public List<Objective> Objectives = new List<Objective>();

		public ObjectiveStatus Status {
			get {
				ObjectiveStatus status = ObjectiveStatus.Completed;

				foreach (Objective objective in Objectives) {

					if (objective == null)
						continue;

					if (objective.Status == ObjectiveStatus.InProgress) {
						status = ObjectiveStatus.InProgress;
					}

					if (!AreOptional && objective.Status == ObjectiveStatus.Failed)
						return ObjectiveStatus.Failed;
				}

				return status;
			}
		}

		public void OnPostLevelLoaded(PlayerStatesContext context)
		{
			foreach(Objective objective in Objectives) {
				if (objective == null)
					continue;

				objective.OnPostLevelLoaded(context);
			}
		}

		public void OnPreLevelUnloading()
		{
			foreach (Objective objective in Objectives) {
				if (objective == null)
					continue;

				objective.OnPreLevelUnloading();
			}
		}

		public void Validate(UnityEngine.Object context)
		{
			foreach (Objective objective in Objectives) {
				if (objective == null)
					continue;

				objective.Validate(context);
			}
		}
	}
}