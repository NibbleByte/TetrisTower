using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerUI
{
	public class ActivateOnRunningStateUIController : MonoBehaviour, ILevelLoadedListener
	{
		public TowerLevelRunningState ActivateOnState;

		[Tooltip("Used when target state is Finished")]
		public bool HasWon;

		public GameObject[] ActivatedObjects;

		private GridLevelController m_TowerLevel;

		private GridLevelData m_LevelData => m_TowerLevel?.LevelData;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.TrySetByType(out m_TowerLevel);
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel = null;
		}

		void Update()
		{
			bool activate = false;

			if (m_LevelData?.RunningState == ActivateOnState) {
				if (ActivateOnState != TowerLevelRunningState.Finished || m_LevelData.HasWon == HasWon) {
					activate = true;
				}
			}

			foreach (GameObject obj in ActivatedObjects) {
				if (obj) {
					obj.SetActive(activate);
				}
			}
		}
	}

}