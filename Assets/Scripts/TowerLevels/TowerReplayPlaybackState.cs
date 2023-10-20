using DevLocker.GFrame.MessageBox;
using TetrisTower.Game;
using TetrisTower.Logic;
using DevLocker.GFrame.Input;
using TetrisTower.TowerUI;
using System.Linq;

namespace TetrisTower.TowerLevels
{
	public class TowerReplayPlaybackState : IPlayerState
	{
		private PlayerControls m_PlayerControls;
		private IInputContext m_InputContext;
		private GridLevelController m_LevelController;
		private TowerLevelUIController m_UIController;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_UIController);
			context.SetByType(out m_InputContext);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Submit);
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(TowerLevelUIState.Play);
			m_UIController.SetIsLevelPlaying(m_LevelController.LevelData.IsPlaying);

			MessageBox.Instance.MessageShown += PauseLevel;
			MessageBox.Instance.MessageClosed += ResumeLevel;

			// Disable player input during playback.
			var inputActions = m_PlayerControls.Where(a => a.actionMap != m_PlayerControls.TowerLevelPlay.Get());
			m_InputContext.PushOrSetActionsMask(this, inputActions);
		}

		public void ExitState()
		{
			m_PlayerControls.DisableAll(this);

			m_InputContext.PopActionsMask(this);

			MessageBox.Instance.MessageShown -= PauseLevel;
			MessageBox.Instance.MessageClosed -= ResumeLevel;
		}

		private void PauseLevel()
		{
			m_LevelController.PauseLevel();
		}

		private void ResumeLevel()
		{
			m_LevelController.ResumeLevel();
		}
	}
}