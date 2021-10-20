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

		[JsonProperty] public int TotalClearedBlocksCount { get; private set; }

		public int CurrentClearedBlocksCount { get; private set; }
		public int CurrentClearActionsCount { get; private set; }

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

			if ((Rules.MatchScoring & action.MatchedType) != 0) {
				CurrentClearedBlocksCount += action.Coords.Count;
				TotalClearedBlocksCount += action.Coords.Count;
				CurrentClearActionsCount += 1;
			}
		}

		private void MatchingSequenceFinished(MatchingSequenceFinishAction action)
		{
			m_Score += CurrentClearedBlocksCount * CurrentClearActionsCount;
			CurrentClearedBlocksCount = 0;
			CurrentClearActionsCount = 0;
		}
	}
}