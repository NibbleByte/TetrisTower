using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class LightningOnMatchComboTrigger : MonoBehaviour, ILevelLoadedListener
	{
		public LightningEffectsController Effects;

		[Tooltip("Trigger lightning only when cascade combo multiplier is above this number.")]
		[Range(0, 20)]
		public int CascadeComboMultiplier = 3;

		private ConeVisualsGrid m_VisualsGrid;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_VisualsGrid);

			m_VisualsGrid.ScoreUpdated += OnUpdateScore;
		}

		public void OnLevelUnloading()
		{
			m_VisualsGrid.ScoreUpdated -= OnUpdateScore;
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

		private void OnUpdateScore(ScoreGrid scoreGrid)
		{
			if (scoreGrid.LastMatchBonus.CascadeBonusMultiplier >= CascadeComboMultiplier) {
				Effects.SpawnLightningStrike();
			}
		}
	}

}