using DevLocker.GFrame;
using TetrisTower.Logic;
using TetrisTower.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelDebugAPI : MonoBehaviour, ILevelLoadedListener
	{
		public static string __DebugInitialTowerLevel;

		public DebugInfoDisplay DebugDisplayInfo;
		public GameObject ProfilerStatsPrefab;

		private GameObject m_ProfilerStatsObject;

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

		public void Win()
		{
			m_TowerLevel.FinishLevel(TowerLevelRunningState.Won);
		}

		public void Lose()
		{
			m_TowerLevel.FinishLevel(TowerLevelRunningState.Lost);
		}

		public void ToggleDebug()
		{
			if (m_ProfilerStatsObject == null && ProfilerStatsPrefab) {
				m_ProfilerStatsObject = Instantiate(ProfilerStatsPrefab);
				return;
			}

			if (m_ProfilerStatsObject == null) {
				DebugDisplayInfo.gameObject.SetActive(!DebugDisplayInfo.gameObject.activeSelf);
				return;
			}

			if (!DebugDisplayInfo.gameObject.activeSelf && !m_ProfilerStatsObject.activeSelf) {
				m_ProfilerStatsObject.SetActive(true);

			} else if (m_ProfilerStatsObject.activeSelf) {
				m_ProfilerStatsObject.SetActive(false);
				DebugDisplayInfo.gameObject.SetActive(true);
			} else {
				DebugDisplayInfo.gameObject.SetActive(false);
			}

		}

		void Update()
		{
			if (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame) {
				ToggleFalling();
			}
		}
	}
}