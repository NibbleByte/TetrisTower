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
		public struct MatchBonus
		{
			public int ClearedBlocks;
			public int CascadeBonusMultiplier;
			public int ResultBonusScore;
		}

		public delegate void ClearActionScoredEvent(ClearMatchedAction action);
		public event ClearActionScoredEvent ClearActionScored;

		[JsonProperty] public int Rows { get; private set; }

		[JsonProperty] public int Columns { get; private set; }

		[JsonProperty] public GridRules Rules { get; private set; }

		[JsonProperty] [UnityEngine.SerializeField]
		private int m_Score = 0;
		public int Score => m_Score;


		[JsonProperty] public int TotalMatched { get; private set; }	// These are matched, which can be more than the actually cleared blocks (when overlapping).
		[JsonProperty] public int TotalClearedBlocksCount { get; private set; }

		public float BonusRatio => TotalClearedBlocksCount != 0 ? (float)Score / TotalClearedBlocksCount : 1f;

		private int m_CurrentClearedBlocksCount = 0;
		private int m_CurrentCascadesCount = 0;

		public MatchBonus LastMatchBonus { get; private set; }

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

		public void ClearLastMatchBonus()
		{
			LastMatchBonus = new MatchBonus();
		}

		public IEnumerator ApplyActions(IEnumerable<GridAction> actions)
		{
			var clearedCoords = new HashSet<GridCoords>();

			bool anyClears = false;
			foreach (var action in actions) {
				switch(action) {
					case ClearMatchedAction clearAction:
						ScoreMatchedCells(clearAction, clearedCoords);
						anyClears = true;
						break;

					case EvaluationSequenceFinishAction finishAction:
						EvaluationSequenceFinished(finishAction);
						anyClears = false;	// Handled.
						break;
				}
			}

			// Next set of actions are considered for cascade bonus.
			if (anyClears) {
				MatchingSequenceFinished();

				TotalClearedBlocksCount += clearedCoords.Count;
			}

			yield break;
		}

		private void ScoreMatchedCells(ClearMatchedAction action, HashSet<GridCoords> totalClearedCoords)
		{
			if (action.MatchedType == 0 && !action.SpecialMatch) {
				Debug.LogError($"{nameof(ClearMatchedAction)} doesn't have a MatchedType set.");
			}

			TotalMatched += action.Coords.Count;

			foreach(GridCoords coord in action.Coords) {
				totalClearedCoords.Add(coord);
			}

			if (!action.SpecialMatch) {

				int maxMatch = Rules.GetMatchLength(action.MatchedType);

				// Having 4 or 5 in a row should be considered as 2x3 or 3x3 respectively.
				m_CurrentClearedBlocksCount += maxMatch * (action.Coords.Count - maxMatch + 1);

			} else {

				// Take it as is.
				m_CurrentClearedBlocksCount += action.Coords.Count;
			}

			// This happens after score is calculated - can't subscribe to BlocksGrid directly.
			ClearActionScored?.Invoke(action);
		}

		private void MatchingSequenceFinished()
		{
			LastMatchBonus = new MatchBonus {
				ClearedBlocks = m_CurrentClearedBlocksCount,
				CascadeBonusMultiplier = m_CurrentCascadesCount + 1,
				ResultBonusScore = m_CurrentClearedBlocksCount * (m_CurrentCascadesCount + 1),
			};

			m_Score += LastMatchBonus.ResultBonusScore;
			m_CurrentClearedBlocksCount = 0;
			m_CurrentCascadesCount++;
		}

		private void EvaluationSequenceFinished(EvaluationSequenceFinishAction action)
		{
			MatchingSequenceFinished();
			m_CurrentCascadesCount = 0;
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