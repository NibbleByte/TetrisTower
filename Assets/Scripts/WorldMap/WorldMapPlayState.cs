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
	public class WorldMapPlayState : IPlayerState, IUpdateListener, PlayerControls.IWorldMapPlayActions
	{
		private PlayerControls m_PlayerControls;
		private GameConfig m_GameConfig;
		private WorldMapController m_LevelController;

		private Vector2 m_MovementInput;
		private Vector2 m_MovementCurrent;
		private Vector2 m_MovementVelocity;
		private float m_ZoomInput;
		private float m_ZoomCurrent;
		private float m_ZoomVelocity;

		private InputEnabler m_InputEnabler;

		public Task EnterStateAsync(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_GameConfig);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.WorldMapPlay);
			m_PlayerControls.WorldMapPlay.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_InputEnabler.Disable(m_PlayerControls.UI.Submit);
			m_InputEnabler.Disable(m_PlayerControls.UI.Navigate);

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.WorldMapPlay.SetCallbacks(null);
			m_InputEnabler.Dispose();

			return Task.CompletedTask;
		}


		public void OnDiscworldMovement(InputAction.CallbackContext context)
		{
			m_MovementInput = context.ReadValue<Vector2>();
		}

		public void OnDiscworldZoom(InputAction.CallbackContext context)
		{
			m_ZoomInput = context.ReadValue<float>();
		}

		public void Update()
		{
			m_MovementCurrent = Vector2.SmoothDamp(m_MovementCurrent, m_MovementInput, ref m_MovementVelocity, 0.3f, 1f);
			m_ZoomCurrent = Mathf.SmoothDamp(m_ZoomCurrent, m_ZoomInput, ref m_ZoomVelocity, 0.3f, 1f);

			m_LevelController.RotateDiscworld(m_MovementCurrent.x);
			m_LevelController.MoveCamera(m_MovementCurrent.y);
			m_LevelController.ZoomCamera(m_ZoomCurrent);
		}
	}
}