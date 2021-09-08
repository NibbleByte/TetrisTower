using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System.Collections;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerPlayState : ILevelState, IUpdateListener, PlayerControls.ITowerLevelPlayActions
	{
		private PlayerControls m_PlayerControls;
		private GameConfig m_GameConfig;
		private PlayerOptions m_Options;
		private TowerLevelController m_LevelController;
		private TowerLevelUIController m_UIController;

		private bool m_PointerPressed = false;
		private bool m_PointerDragConsumed = false;
		private Vector2 m_PointerPressedStartPosition;
		private Vector2 m_PointerPressedLastPosition;
		private float m_PointerPressedTime;

		public IEnumerator EnterState(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_GameConfig);
			contextReferences.SetByType(out m_Options);

			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.TowerLevelPlay.SetCallbacks(this);
			m_PlayerControls.TowerLevelPlay.Enable();

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
					m_PointerPressedStartPosition = Pointer.current.position.ReadValue();
					m_PointerPressedLastPosition = m_PointerPressedStartPosition;
					m_PointerPressedTime = Time.time;
					m_PointerPressed = true;
					m_PointerDragConsumed = false;
					break;

				case InputActionPhase.Canceled:

					Debug.Assert(m_PointerPressed);
					m_PointerPressed = false;
					m_PointerDragConsumed = false;
					m_LevelController.ClearFallingShapeAnalogMoveOffset();
					m_LevelController.ClearFallingShapeAnalogRotateOffset();

					if (m_Options.TouchInputControls != PlayerOptions.TouchInputControlMethod.Swipes)
						break;

					var pressDuration = Time.time - m_PointerPressedTime;
					var pressDistance = Pointer.current.position.ReadValue() - m_PointerPressedStartPosition;

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
							m_LevelController.RequestFallingShapeMove(-1);
						}

						if (Vector2.Dot(Vector2.left, pressDir) >= m_GameConfig.SwipeConformity) {
							m_LevelController.RequestFallingShapeMove(+1);
						}
					}
					break;

				case InputActionPhase.Disabled:
				case InputActionPhase.Waiting:
					m_PointerPressed = false;
					m_LevelController.ClearFallingShapeAnalogMoveOffset();
					break;

			}
		}

		public void Update()
		{
			if (Pointer.current == null)
				return;

			if (m_PointerPressed && m_Options.TouchInputControls == PlayerOptions.TouchInputControlMethod.Drag) {

				var currentPosition = Pointer.current.position.ReadValue();
				var dragDistance = currentPosition - m_PointerPressedLastPosition;
				m_PointerPressedLastPosition = currentPosition;

				if (!float.IsNaN(m_LevelController.FallingColumnAnalogOffset)) {
					if (Mathf.Abs(dragDistance.x) > 0.01f) {
						m_LevelController.AddFallingShapeAnalogMoveOffset(-dragDistance.x * 0.025f / InputMetrics.InputPrecision);
					}

				} else if (!float.IsNaN(m_LevelController.FallingShapeAnalogRotateOffset)) {
					if (Mathf.Abs(dragDistance.y) > 0.01f) {
						m_LevelController.AddFallingShapeAnalogRotateOffset(dragDistance.y * 0.020f / InputMetrics.InputPrecision);
					}

				// Avoid starting another drag operation if the last one was interrupted.
				} else if (!m_PointerDragConsumed) {
					dragDistance = currentPosition - m_PointerPressedStartPosition;

					if (dragDistance.magnitude > 6f * InputMetrics.InputPrecision && Time.time - m_PointerPressedTime > 0.1f) {
						m_PointerDragConsumed = true;
						m_PlayerControls.TowerLevelPlay.PointerFallSpeedUp.Reset();

						if (Mathf.Abs(dragDistance.x) > Mathf.Abs(dragDistance.y)) {
							m_LevelController.AddFallingShapeAnalogMoveOffset(-dragDistance.x * 0.025f / InputMetrics.InputPrecision);
						} else {
							m_LevelController.AddFallingShapeAnalogRotateOffset(dragDistance.y * 0.020f / InputMetrics.InputPrecision);
						}
					}
				}
			}
		}
	}
}