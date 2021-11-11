using TetrisTower.Logic;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Used by the visuals grids, to notify about matching sequence score happening.
	/// </summary>
	public interface IMatchSequenceScoreDisplayer
	{
		void UpdateScore(ScoreGrid scoreGrid);
		void FinishScore(ScoreGrid scoreGrid);
	}
}
