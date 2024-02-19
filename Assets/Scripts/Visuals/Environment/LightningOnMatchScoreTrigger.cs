using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class LightningOnMatchScoreTrigger : MonoBehaviour, ILevelLoadedListener
	{
		public LightningEffectsController Effects;

		[Tooltip("Trigger lightning when match score is equal or above this number.")]
		[Range(0, 20)]
		public int MatchScore = 7;

		private ConeVisualsGrid m_VisualsGrid;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_VisualsGrid);

			m_VisualsGrid.ScoreFinished += OnScoreFinished;
		}

		public void OnLevelUnloading()
		{
			m_VisualsGrid.ScoreFinished -= OnScoreFinished;
			m_VisualsGrid = null;
		}

		void OnValidate()
		{
#if UNITY_EDITOR
			if (Effects == null && !Application.isPlaying) {
				Effects = GetComponent<LightningEffectsController>();
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif

			if (Effects == null) {
				Debug.LogError($"{typeof(LightningOnMatchComboTrigger)} \"{name}\" has no {typeof(LightningEffectsController)} set.", this);
			}
		}

		private void OnScoreFinished(ScoreGrid scoreGrid)
		{
			if (scoreGrid.Score >= MatchScore) {
				Effects.SpawnLightningStrike();
			}
		}
	}

}