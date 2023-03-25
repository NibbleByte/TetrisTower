using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPausedState : ILevelState, PlayerControls.ITowerLevelPausedActions
	{
		private GridLevelController m_LevelController;
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;

		private InputEnabler m_InputEnabler;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.TowerLevelPaused);
			m_PlayerControls.TowerLevelPaused.SetCallbacks(this);

			m_UIController.SwitchState(UI.TowerLevelUIState.Paused);

			m_LevelController.PauseLevel();

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_LevelController.ResumeLevel();

			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_InputEnabler.Dispose();

			return Task.CompletedTask;
		}
	}
}