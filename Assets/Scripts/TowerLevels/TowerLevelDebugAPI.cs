using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelDebugAPI : MonoBehaviour, ILevelLoadedListener
	{
		public static string __DebugInitialTowerLevel;

		private float m_FallSpeedOriginal;
		private GridLevelController m_TowerLevel;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStaticsCache()
		{
			__DebugInitialTowerLevel = string.Empty;
		}

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);
		}

		public void OnLevelUnloading()
		{
		}

		public void ToggleFalling()
		{
			if (m_TowerLevel.LevelData.FallSpeedNormalized == 0f) {
				m_TowerLevel.LevelData.FallSpeedNormalized = m_FallSpeedOriginal;
			} else {
				m_FallSpeedOriginal = m_TowerLevel.LevelData.FallSpeedNormalized;
				m_TowerLevel.LevelData.FallSpeedNormalized = 0f;
			}
		}

		public void ResetLevel()
		{
			UI.TowerLevelUIController.RetryLevel();
		}

		void Update()
		{
			if (Keyboard.current.pKey.wasPressedThisFrame) {
				ToggleFalling();
			}
		}
	}
}