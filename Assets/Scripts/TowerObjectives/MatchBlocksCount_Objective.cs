using DevLocker.GFrame.Input;
using DevLocker.GFrame.Utils;
using Newtonsoft.Json;
using System;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerObjectives
{
	/// <summary>
	/// Objective is completed when specified <see cref="MatchesEndCount"/> number of matches is made.
	/// Can specify the type of matches <see cref="MatchesType"/> as well.
	/// Cannot fail.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class MatchBlocksCount_Objective : Objective
	{
		[Tooltip("How much matches (according to the rules) does the player has to do to pass this level. 0 means it is an endless game.")]
		public int MatchesEndCount;

		[EnumMask]
		public MatchScoringType MatchesType = MatchScoringType.Horizontal | MatchScoringType.Vertical | MatchScoringType.Diagonals;
		public bool MatchesAllTypes =>
			MatchesType == (MatchScoringType)~0 ||
			MatchesType == (MatchScoringType.Horizontal | MatchScoringType.Vertical | MatchScoringType.Diagonals)
			;


		[field: JsonProperty(nameof(Status))]
		public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.InProgress;

		private int m_MatchesDone;
		public int MatchesDone => m_MatchesDone;

		[JsonIgnore]
		private ScoreGrid m_ScoreGrid;

		[JsonIgnore]
		private GridRules m_Rules;

		[JsonIgnore]
		private ObjectivesPresenter m_Presenter;

		public void OnPostLevelLoaded(PlayerStatesContext context)
		{
			var levelData = context.FindByType<GridLevelData>();
			m_ScoreGrid = levelData.Score;
			m_ScoreGrid.ClearActionScored += OnClearActionScored;
			m_Rules = levelData.Rules;

			m_Presenter = context.TryFindByType<ObjectivesPresenter>();
			TryProcessGreetMessage();

			TryDisplayObjective();
		}

		public void OnPreLevelUnloading()
		{
			m_ScoreGrid.ClearActionScored -= OnClearActionScored;
		}

		private void OnClearActionScored(ClearMatchedAction action)
		{
			if (!action.SpecialMatch) {

				if ((MatchesType & action.MatchedType) != 0) {
					if (MatchesAllTypes) {
						m_MatchesDone += action.Coords.Count;
					} else {
						int maxMatch = m_Rules.GetMatchLength(action.MatchedType);

						int clearActionsCount = Mathf.Max(action.Coords.Count - maxMatch + 1, 1);
						m_MatchesDone += clearActionsCount;
					}
				}

			} else {

				if (MatchesAllTypes) {
					m_MatchesDone += action.Coords.Count;
				}
			}

			if (m_MatchesDone >= MatchesEndCount) {
				if (Status == ObjectiveStatus.InProgress) {
					Status = ObjectiveStatus.Completed;
					if (Application.isPlaying) {
						Debug.Log($"Remaining blocks to clear are 0. Objective completed.");
					}
				}
				m_MatchesDone = MatchesEndCount;
			}

			TryDisplayObjective();
		}

		private void TryDisplayObjective()
		{
			if (m_Presenter == null)
				return;

			string text = $"Remaining: {MatchesEndCount - m_MatchesDone}";

			if (!MatchesAllTypes) {
				text += "\n<color=\"orange\"><b>";

				if (MatchesType.HasFlag(MatchScoringType.Horizontal)) {
					text += "Horizontal ";
				}
				if (MatchesType.HasFlag(MatchScoringType.Vertical)) {
					text += "Vertical ";
				}
				if (MatchesType.HasFlag(MatchScoringType.Diagonals)) {
					text += "Diagonals ";
				}

				text += "Only!</b></color>";
			}

			m_Presenter.SetObjectiveText(this, text);
		}

		private void TryProcessGreetMessage()
		{
			if (m_Presenter == null)
				return;

			string message = m_Presenter.GreetingMessage;

			if (string.IsNullOrWhiteSpace(message)) {
				if (!MatchesAllTypes) {
					message = "";
					if (MatchesType.HasFlag(MatchScoringType.Horizontal)) {
						message += "Horizontal ";
					}
					if (MatchesType.HasFlag(MatchScoringType.Vertical)) {
						message += "Vertical ";
					}
					if (MatchesType.HasFlag(MatchScoringType.Diagonals)) {
						message += "Diagonals ";
					}

					message += "Only!";
					m_Presenter.GreetingMessage = message;
					return;
				}
			}


			m_Presenter.GreetingMessage = message.Replace("{ObjectiveEndCount}", MatchesEndCount.ToString());
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}