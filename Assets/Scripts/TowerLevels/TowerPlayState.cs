using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System.Collections;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPlayState : ILevelState, PlayerControls.ITowerLevelPlayActions
	{
		private PlayerControls m_PlayerControls;
		private GameConfig m_GameConfig;
		private TowerLevelController m_LevelController;
		private TowerLevelUIController m_UIController;

		private Vector2 m_PointerPressedPosition;
		private double m_PointerPressedTime;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_GameConfig);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.CommonHotkeys.Enable();
			m_PlayerControls.TowerLevelPlay.SetCallbacks(this);
			m_PlayerControls.TowerLevelPlay.Enable();
			m_PlayerControls.TowerLevelShared.Enable();

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.UI.Submit.Disable();
			m_PlayerControls.UI.Navigate.Disable();

			m_UIController.SwitchState(TowerLevelUIState.Play);

			m_LevelController.ResumeLevel();

			MessageBox.Instance.MessageShown += m_LevelController.PauseLevel;
			MessageBox.Instance.MessageClosed += m_LevelController.ResumeLevel;

			yield break;
		}

		public IEnumerator ExitState()
		{
			m_LevelController.PauseLevel();

			m_PlayerControls.TowerLevelPlay.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			MessageBox.Instance.MessageShown -= m_LevelController.PauseLevel;
			MessageBox.Instance.MessageClosed -= m_LevelController.ResumeLevel;

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
				m_LevelController.RequestFallingShapeRotate(+1);
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

		// Pointer (touch or mouse) version, as binding interactions get ignored if grouped with other devices.
		public void OnPointerFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_LevelController.RequestFallingSpeedUp(m_GameConfig.FallSpeedup);

				// Avoid executing both actions, because they are set as "Pass through".
				m_PlayerControls.TowerLevelPlay.PointerPress.Reset();
			}
		}

		// Pointer (touch or mouse) gesture detections.
		public void OnPointerPress(InputAction.CallbackContext context)
		{
			if (Pointer.current == null)
				return;

			switch (context.phase) {
				case InputActionPhase.Started:
					m_PointerPressedPosition = Pointer.current.position.ReadValue();
					m_PointerPressedTime = context.time;
					break;

				case InputActionPhase.Canceled:
					var pressDuration = context.time - m_PointerPressedTime;
					var pressDistance = Pointer.current.position.ReadValue() - m_PointerPressedPosition;

					// Swipe detection.
					if (pressDuration < m_GameConfig.SwipeMaxTime && pressDistance.magnitude >= m_GameConfig.SwipeMinDistance) {
						var pressDir = pressDistance.normalized;

						// Avoid executing both actions, because they are set as "Pass through".
						m_PlayerControls.TowerLevelPlay.PointerFallSpeedUp.Reset();

						//Debug.Log($"Swipe: time - {pressDuration}, dist - {pressDistance.magnitude}");

						if (Vector2.Dot(Vector2.up, pressDir) >= m_GameConfig.SwipeConformity) {
							m_LevelController.RequestFallingShapeRotate(+1);
						}

						if (Vector2.Dot(Vector2.down, pressDir) >= m_GameConfig.SwipeConformity) {
							m_LevelController.RequestFallingShapeRotate(-1);
						}

						if (Vector2.Dot(Vector2.right, pressDir) >= m_GameConfig.SwipeConformity) {
							m_LevelController.RequestFallingShapeMove(+1);
						}

						if (Vector2.Dot(Vector2.left, pressDir) >= m_GameConfig.SwipeConformity) {
							m_LevelController.RequestFallingShapeMove(-1);
						}
					}
					break;
			}
		}
	}
}