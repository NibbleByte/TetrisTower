using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Logic
{
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	public class ScoreGrid : GameGrid
	{
		[JsonProperty] public int Rows { get; private set; }

		[JsonProperty] public int Columns { get; private set; }

		[JsonProperty] public GridRules Rules { get; private set; }

		[JsonProperty] [UnityEngine.SerializeField]
		private int m_Score = 0;
		public int Score => m_Score;

		[JsonProperty] public int ObjectiveProgress { get; private set; }

		public int CurrentClearedBlocksCount { get; private set; }
		public int CurrentClearActionsCount { get; private set; }

		public int BonusScore { get; private set; } = 0;

		// For serialization.
		public ScoreGrid()
		{
		}

		public ScoreGrid(int rows, int columns, GridRules rules)
		{
			Rows = rows;
			Columns = columns;
			Rules = rules;
		}

		public IEnumerator ApplyActions(IEnumerable<GridAction> actions)
		{
			foreach (var action in actions) {
				switch(action) {
					case ClearMatchedAction clearAction:
						ScoreMatchedCells(clearAction);
						break;

					case MatchingSequenceFinishAction finishAction:
						MatchingSequenceFinished(finishAction);
						break;
				}
			}

			yield break;
		}

		public void ScoreMatchedCells(ClearMatchedAction action)
		{
			if (action.MatchedType == 0) {
				Debug.LogError($"{nameof(ClearMatchedAction)} doesn't have a MatchedType set.");
			}

			int maxMatch = 0;
			switch(action.MatchedType) {
				case MatchScoringType.Horizontal: maxMatch = Rules.MatchHorizontalLines; break;
				case MatchScoringType.Vertical: maxMatch = Rules.MatchVerticalLines; break;
				case MatchScoringType.Diagonals: maxMatch = Rules.MatchDiagonalsLines; break;
			}

			// Having 4 or 5 in a row should be considered as 2x3 or 3x3 respectively.
			CurrentClearedBlocksCount = Mathf.Max(CurrentClearedBlocksCount, maxMatch);
			int clearActionsCount = Mathf.Max(action.Coords.Count - maxMatch + 1, 1);
			CurrentClearActionsCount += clearActionsCount;

			if ((Rules.ObjectiveType & action.MatchedType) != 0) {
				if (Rules.IsObjectiveAllMatchTypes) {
					ObjectiveProgress += action.Coords.Count;
				} else {
					ObjectiveProgress += clearActionsCount;
				}
			}
		}

		private void MatchingSequenceFinished(MatchingSequenceFinishAction action)
		{
			m_Score += CurrentClearedBlocksCount * CurrentClearActionsCount;
			CurrentClearedBlocksCount = 0;
			CurrentClearActionsCount = 0;
		}

		public void IncrementWonBonusScore()
		{
			BonusScore++;
		}

		public void ConsumeBonusScore()
		{
			m_Score += BonusScore;
			BonusScore = 0;
		}
	}
}