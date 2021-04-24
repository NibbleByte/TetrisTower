using System.Collections;
using System.Collections.Generic;
using TetrisTower.Levels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Input
{
	public class LevelInputController : MonoBehaviour, PlayerControls.ILevelGameActions
	{
		public LevelController LevelController { get; private set; }
		public LevelData LevelData => LevelController?.LevelData;

		public float FallSpeedup = 40f;

		private System.Action m_PauseRequestHandler;

		public void Init(LevelController levelController, System.Action pauseRequestHandler)
		{
			LevelController = levelController;

			m_PauseRequestHandler = pauseRequestHandler;
		}

		public void OnMoveShapeLeft(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelController.RequestFallingShapeMove(-1);
			}
		}

		public void OnMoveShapeRight(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelController.RequestFallingShapeMove(+1);
			}
		}

		public void OnRotateShapeUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelController.RequestFallingShapeRotate(1);
			}
		}

		public void OnRotateShapeDown(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelController.RequestFallingShapeRotate(-1);
			}
		}

		public void OnFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				LevelController.RequestFallingSpeedUp(FallSpeedup);
			}
		}

		public void OnPauseLevel(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_PauseRequestHandler();
			}
		}
	}
}