using DevLocker.GFrame;
using System.Collections;
using TetrisTower.Input;

namespace TetrisTower.TowerLevels
{
	public class TowerOptionsState : ILevelState
	{
		private TowerLevelUIController m_UIController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);

			m_UIController.SwitchState(TowerLevelUIState.Options);

			yield break;
		}

		public IEnumerator ExitState()
		{
			yield break;
		}
	}
}