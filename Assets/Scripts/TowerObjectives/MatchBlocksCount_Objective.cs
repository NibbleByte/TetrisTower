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


		public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.InProgress;

		private int m_MatchesDone;
		public int MatchesDone => m_MatchesDone;

		[JsonIgnore]
		private GridRules m_Rules;

		public void Init(GridLevelController levelController)
		{
			levelController.LevelData.Score.ClearActionScored += OnClearActionScored;
			m_Rules = levelController.LevelData.Rules;
		}

		public void Deinit(GridLevelController levelController)
		{
			levelController.LevelData.Score.ClearActionScored -= OnClearActionScored;
		}

		// For unit tests.
		public void Init(GridRules rules, ScoreGrid score)
		{
			score.ClearActionScored += OnClearActionScored;
			m_Rules = rules;
		}

		// For unit tests.
		public void Deinit(ScoreGrid score)
		{
			score.ClearActionScored -= OnClearActionScored;
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
		}

		public string GetDisplayText()
		{
			string text = $"Remaining: {MatchesEndCount - m_MatchesDone}\n";

			if (!MatchesAllTypes) {
				text += "<color=\"orange\"><b>";

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

			return text;
		}

		public string ProcessGreetMessage(string message)
		{
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

					return message;
				}
			}


			return message
				.Replace("{ObjectiveEndCount}", MatchesEndCount.ToString())
				;
		}

		public void Validate(UnityEngine.Object context)
		{
		}
	}
}