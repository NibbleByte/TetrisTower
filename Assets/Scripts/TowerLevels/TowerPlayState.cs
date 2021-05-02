using System.Collections;
using TetrisTower.Core;
using TetrisTower.Game;
using TetrisTower.Input;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPlayState : ILevelState, PlayerControls.ITowerLevelGameActions
	{
		private PlayerControls m_PlayerControls;
		private GameConfig m_GameConfig;
		private TowerLevelController m_LevelController;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_GameConfig);

			m_PlayerControls.TowerLevelGame.SetCallbacks(this);
			m_PlayerControls.TowerLevelGame.Enable();

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_PlayerControls.TowerLevelGame.SetCallbacks(null);
			m_PlayerControls.TowerLevelGame.Disable();

			yield break;
		}

		public void OnMoveShapeLeft(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingShapeMove(-1);
			}
		}

		public void OnMoveShapeRight(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingShapeMove(+1);
			}
		}

		public void OnRotateShapeUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingShapeRotate(1);
			}
		}

		public void OnRotateShapeDown(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingShapeRotate(-1);
			}
		}

		public void OnFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingSpeedUp(m_GameConfig.FallSpeedup);
			}
		}

		public void OnPauseLevel(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelSupervisorsManager.Instance.SetLevelState(new TowerPausedState());
			}
		}
	}
}