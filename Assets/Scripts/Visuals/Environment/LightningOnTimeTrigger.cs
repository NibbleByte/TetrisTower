using DevLocker.GFrame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class LightningOnTimeTrigger : MonoBehaviour, ILevelLoadedListener
	{
		public LightningEffectsController Effects;

		[Tooltip("Should start all over after script has ran out of entries?")]
		public bool Loop = false;

		[Tooltip("How many seconds after the level start should the lightning be spawned?")]
		public float[] LightningSecondsSinceStart;

		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private int m_LastTimeIndex = 0;
		private int m_LoopCount = 0;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);

			Array.Sort(LightningSecondsSinceStart);
			m_LastTimeIndex = 0;
			m_LoopCount = 0;

			if (LightningSecondsSinceStart.Length == 0) {
				Debug.LogWarning($"{typeof(LightningOnTimeTrigger)} has empty list of times.", this);
				enabled = false;
			}

		}

		public void OnLevelUnloading()
		{
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
				Debug.LogError($"{typeof(LightningOnTimeTrigger)} \"{name}\" has no {typeof(LightningEffectsController)} set.", this);
			}
		}

		private float WrappedPlayTime {
			get {
				float playTime = m_LevelData.PlayTime;

				if (Loop) {
					float totalTime = LightningSecondsSinceStart.Last();
					while(playTime >= totalTime) {
						playTime -= totalTime;
					}
				}

				return playTime;
			}
		}

		void Update()
		{
			if (m_TowerLevel == null)
				return;

			while((Loop || m_LoopCount == 0) && LightningSecondsSinceStart[m_LastTimeIndex] <= (m_LevelData.PlayTime - m_LoopCount * LightningSecondsSinceStart.Last())) {
				m_LastTimeIndex++;
				Effects.SpawnLightningStrike();

				if (m_LastTimeIndex >= LightningSecondsSinceStart.Length) {
					m_LastTimeIndex = 0;
					m_LoopCount++;

					break;
				}
			}
		}
	}

}