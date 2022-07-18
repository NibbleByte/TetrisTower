using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class LightningOnBlockTypesChangedTrigger : MonoBehaviour, ILevelLoadedListener
	{
		public LightningEffectsController Effects;

		[Tooltip("Chance to skip the lightning. 0 means no skips.")]
		public int SkipChance = 0;

		private GridLevelController m_TowerLevel;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);

			m_TowerLevel.SpawnBlockTypesCountChanged += OnSpawnBlockTypesCountChanged;
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.SpawnBlockTypesCountChanged -= OnSpawnBlockTypesCountChanged;
			m_TowerLevel = null;
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
				Debug.LogError($"{typeof(LightningOnBlockTypesChangedTrigger)} \"{name}\" has no {typeof(LightningEffectsController)} set.", this);
			}
		}

		private void OnSpawnBlockTypesCountChanged()
		{
			if (Random.Range(0, SkipChance) == 0) {
				Effects.SpawnLightningStrike();
			}
		}
	}

}