using DevLocker.GFrame.MessageBox;
using System.Collections;
using System.Threading.Tasks;
using TetrisTower.Platforms;
using TetrisTower.Core.UI;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using DevLocker.GFrame.Input;
using DevLocker.GFrame;

namespace TetrisTower.WorldMap
{
	public class WorldMapPausedState : IPlayerState, PlayerControls.IWorldMapPausedActions
	{
		private PlayerControls m_PlayerControls;
		private UI.WorldMapUIController m_UIController;

		private InputEnabler m_InputEnabler;

		public Task EnterStateAsync(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.WorldMapPaused);
			m_PlayerControls.WorldMapPaused.SetCallbacks(this);

			m_UIController.SwitchState(UI.WorldMapUIState.Paused);

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.TowerLevelPaused.SetCallbacks(null);
			m_InputEnabler.Dispose();

			return Task.CompletedTask;
		}
	}
}