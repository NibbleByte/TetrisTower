using System.Collections;
using System.Collections.Generic;
using TetrisTower.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelInputController : MonoBehaviour, PlayerControls.ILevelGameActions
	{
		public TowerLevelController TowerLevel { get; private set; }
		public TowerLevelData LevelData => TowerLevel?.LevelData;

		public float FallSpeedup = 40f;

		private System.Action m_PauseRequestHandler;

		public void Init(TowerLevelController towerLevel, System.Action pauseRequestHandler)
		{
			TowerLevel = towerLevel;

			m_PauseRequestHandler = pauseRequestHandler;
		}

		public void OnMoveShapeLeft(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				TowerLevel.RequestFallingShapeMove(-1);
			}
		}

		public void OnMoveShapeRight(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				TowerLevel.RequestFallingShapeMove(+1);
			}
		}

		public void OnRotateShapeUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				TowerLevel.RequestFallingShapeRotate(1);
			}
		}

		public void OnRotateShapeDown(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				TowerLevel.RequestFallingShapeRotate(-1);
			}
		}

		public void OnFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				TowerLevel.RequestFallingSpeedUp(FallSpeedup);
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