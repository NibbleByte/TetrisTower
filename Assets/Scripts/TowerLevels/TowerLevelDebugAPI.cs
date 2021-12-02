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
			var playthroughData = GameManager.Instance.GameContext.CurrentPlaythrough;
			if (!string.IsNullOrEmpty(__DebugInitialTowerLevel)) {
				var config = GameManager.Instance.GameContext.GameConfig;

				playthroughData.TowerLevel = Newtonsoft.Json.JsonConvert.DeserializeObject<GridLevelData>(__DebugInitialTowerLevel, config.Converters);
				GameManager.Instance.SwitchLevel(new TowerLevelSupervisor());
				return;
			}

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