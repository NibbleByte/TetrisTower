using DevLocker.GFrame;
using TetrisTower.Platforms;
using TetrisTower.Core.UI;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using DevLocker.GFrame.Input;
using TetrisTower.TowerUI;
using TetrisTower.TowerLevels.Replays;
using TetrisTower.Game.Preferences;

namespace TetrisTower.TowerLevels
{
	public class TowerPlayState : IPlayerState, IUpdateListener, PlayerControls.ITowerLevelPlayActions
	{
		private IPlayerContext m_PlayerContext;
		private PlayerControls m_PlayerControls;
		private GameConfig m_GameConfig;
		private IUserPreferences m_UserPrefs;
		private GridLevelController m_LevelController;
		private TowerLevelUIController m_UIController;
		private ReplayActionsRecording m_ReplayRecording;

		private bool m_PointerPressed = false;
		private bool m_PointerDragConsumed = false;
		private bool m_PointerDragSwiped = false;
		private Vector2 m_PointerPressedStartPosition;
		private Vector2 m_PointerPressedLastPosition;
		private float m_PointerPressedTime;

		private int m_LastUpdateFrame;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerContext);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_UIController);
			context.SetByType(out m_GameConfig);
			context.SetByType(out m_UserPrefs);
			context.SetByType(out m_ReplayRecording);

			m_PlayerControls.Enable(this, m_PlayerControls.UI);
			m_PlayerControls.Enable(this, m_PlayerControls.TowerLevelPlay);
			m_PlayerControls.TowerLevelPlay.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Submit);
			m_PlayerControls.Disable(this, m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(TowerLevelUIState.Play);
			m_UIController.SetIsLevelPlaying(m_LevelController.LevelData.IsPlaying);
		}

		public void ExitState()
		{
			m_PlayerControls.TowerLevelPlay.SetCallbacks(null);
			m_PlayerControls.DisableAll(this);
		}

		public void OnMoveShapeLeft(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Move, -1);
			}
		}

		public void OnMoveShapeRight(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Move, +1);
			}
		}

		public void OnRotateShapeUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Rotate, +1);
			}
		}

		public void OnRotateShapeDown(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				if (m_UserPrefs.DownIsDrop) {
					OnFallSpeedUp(context);
				} else {
					m_ReplayRecording.AddAndRun(ReplayActionType.Rotate, -1);
				}
			}
		}

		public void OnFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_ReplayRecording.AddAndRun(ReplayActionType.FallSpeedUp, m_GameConfig.FallSpeedup);
			}
		}

		// Pointer (touch or mouse) version, as binding interactions get ignored if grouped with other devices.
		public void OnPointerFallSpeedUp(InputAction.CallbackContext context)
		{
			if (context.phase == InputActionPhase.Performed) {
				m_ReplayRecording.AddAndRun(ReplayActionType.FallSpeedUp, m_GameConfig.FallSpeedup);

				// Avoid executing both actions, because they are set as "Pass through".
				m_PlayerControls.TowerLevelPlay.PointerPress.Reset();
			}
		}

		// Pointer (touch or mouse) gesture detections.
		public void OnPointerPress(InputAction.CallbackContext context)
		{
			if (Pointer.current == null)
				return;

			// Level may end while dragging - cancel everything.
			if (m_ReplayRecording.HasEnding)
				return;

			switch (context.phase) {
				case InputActionPhase.Started:
					m_PointerPressedStartPosition = Pointer.current.position.ReadValue();

					// Pressed on the UI.
					if (UIUtils.RaycastUIElements(m_PointerPressedStartPosition).Count > 0)
						return;

					m_PointerPressedLastPosition = m_PointerPressedStartPosition;
					m_PointerPressedTime = Time.time;
					m_PointerPressed = true;
					m_PointerDragConsumed = false;
					m_PointerDragSwiped = false;
					break;

				case InputActionPhase.Canceled:

					if (!m_PointerPressed)
						return;

					// Input handles get called before the Update(), so the last frame doesn't get applied if input is canceled.
					// This is especially noticeable for hyper-fast swipes with duration 1 frame:
					// First frame starts, next frame ends and no Update gets called (including on the first frame?!).
					// Maybe Started is called after update and Canceled before? Or there is some race condition?
					if (m_LastUpdateFrame != Time.frameCount) {
						Update();

						if (m_ReplayRecording.HasEnding)
							return;
					}

					m_PointerPressed = false;
					m_PointerDragConsumed = false;
					m_PointerDragSwiped = false;

					m_ReplayRecording.AddAndRun(ReplayActionType.ClearMoveOffset);
					if (m_ReplayRecording.HasEnding)
						return;

					m_ReplayRecording.AddAndRun(ReplayActionType.ClearRotateOffset);
					if (m_ReplayRecording.HasEnding)
						return;


					var pressDuration = Time.time - m_PointerPressedTime;
					var pressDistance = Pointer.current.position.ReadValue() - m_PointerPressedStartPosition;

					if (m_UserPrefs.TouchInputControls == IUserPreferences.TouchInputControlMethod.Drag) {
						break;
					}


					// Swipe detection.
					if (pressDuration < m_GameConfig.SwipeMaxTime && pressDistance.magnitude >= m_GameConfig.SwipeMinDistance * InputMetrics.InputPrecision) {
						var pressDir = pressDistance.normalized;

						// Avoid executing both actions, because they are set as "Pass through".
						m_PlayerControls.TowerLevelPlay.PointerFallSpeedUp.Reset();

						//Debug.Log($"Swipe: time - {pressDuration}, dist - {pressDistance.magnitude}");

						if (Vector2.Dot(Vector2.up, pressDir) >= m_GameConfig.SwipeConformity) {
							m_ReplayRecording.AddAndRun(ReplayActionType.Rotate, +1);
						}

						if (Vector2.Dot(Vector2.down, pressDir) >= m_GameConfig.SwipeConformity) {
							m_ReplayRecording.AddAndRun(ReplayActionType.Rotate, -1);
						}

						if (Vector2.Dot(Vector2.right, pressDir) >= m_GameConfig.SwipeConformity) {
							m_ReplayRecording.AddAndRun(ReplayActionType.Move, -1);
						}

						if (Vector2.Dot(Vector2.left, pressDir) >= m_GameConfig.SwipeConformity) {
							m_ReplayRecording.AddAndRun(ReplayActionType.Move, +1);
						}
					}
					break;

				case InputActionPhase.Disabled:
				case InputActionPhase.Waiting:
					if (!m_PointerPressed)
						return;

					m_PointerPressed = false;
					m_ReplayRecording.AddAndRun(ReplayActionType.ClearMoveOffset);
					break;

			}
		}

		public void Update()
		{
			if (m_LevelController.LevelData != null && !m_LevelController.LevelData.IsPlaying) {
				// Check for this in Update, rather than the event, in case the level finished while in another state.
				m_PlayerContext.StatesStack.SetState(m_LevelController.LevelData.HasWon ? new TowerWonState() : new TowerLostState());
				return;
			}

			m_LastUpdateFrame = Time.frameCount;

			if (Pointer.current == null)
				return;

			if (m_PointerPressed && m_UserPrefs.TouchInputControls == IUserPreferences.TouchInputControlMethod.Drag) {

				var currentPosition = Pointer.current.position.ReadValue();
				var dragLastDistance = currentPosition - m_PointerPressedLastPosition;
				var dragFullDistance = currentPosition - m_PointerPressedStartPosition;
				var dragDuration = Time.time - m_PointerPressedTime;
				m_PointerPressedLastPosition = currentPosition;

				float currentRotateOffset = m_LevelController.FallingShapeAnalogRotateOffset;
				currentRotateOffset = float.IsNaN(currentRotateOffset) ? 0 : currentRotateOffset;


				// Horizontal drag in progress - move
				if (!float.IsNaN(m_LevelController.FallingColumnAnalogOffset)) {
					if (Mathf.Abs(dragLastDistance.x) > 0.01f) {
						m_ReplayRecording.AddAndRun(ReplayActionType.OffsetMove, -dragLastDistance.x * m_GameConfig.AnalogMoveSpeed / InputMetrics.InputPrecision);
					}

				// Vertical drag in progress - rotate
				} else if (!float.IsNaN(m_LevelController.FallingShapeAnalogRotateOffset)) {

					bool isSwipe = dragDuration < m_GameConfig.SwipeMaxTime
						&& Mathf.Abs(dragFullDistance.y) > m_GameConfig.SwipeMinDistance * InputMetrics.InputPrecision;

					if (Mathf.Abs(dragLastDistance.y) > 0.01f) {

						// Limit rotation to 1 per frame.
						float rotateSign = Mathf.Sign(dragLastDistance.y);
						float rotateValue = rotateSign * Mathf.Min(
							Mathf.Abs(dragLastDistance.y) * m_GameConfig.AnalogRotateSpeed / InputMetrics.InputPrecision,
							0.5f // Rotation is [-0.5, 0.5].
						);

						//Debug.LogError($"Drag with {dragLastDistance.y} ({rotateValue}) at {dragDuration} frame {Time.frameCount}");

						// Offset larger than 0.5f will trigger rotation.
						// If this is a swipe, don't allow more than 1 rotation.
						if (isSwipe && !m_PointerDragSwiped && Mathf.Abs(rotateValue + currentRotateOffset) >= 0.5f) {
							m_PointerDragSwiped = true;
							rotateValue += rotateSign * 0.2f; // 0.5 means visually rotation is half-way. Snap it visually towards the end.
							m_ReplayRecording.AddAndRun(ReplayActionType.OffsetRotate, rotateValue);

							//Debug.LogWarning($"SWIPE! SKIP! {dragFullDistance.y} for {dragDuration}");

						} else if (!isSwipe || !m_PointerDragSwiped) {
							m_ReplayRecording.AddAndRun(ReplayActionType.OffsetRotate, rotateValue);
						}

					}

				// No drag - try starting one.
				// Avoid starting another drag operation if the last one was interrupted.
				} else if (!m_PointerDragConsumed) {

					if (dragFullDistance.magnitude > 10f * InputMetrics.InputPrecision) {
						m_PointerDragConsumed = true;
						m_PlayerControls.TowerLevelPlay.PointerFallSpeedUp.Reset();

						if (Mathf.Abs(dragFullDistance.x) > Mathf.Abs(dragFullDistance.y)) {
							m_ReplayRecording.AddAndRun(ReplayActionType.OffsetMove, -dragFullDistance.x * m_GameConfig.AnalogMoveSpeed / InputMetrics.InputPrecision);
						} else {
							float rotateSign = Mathf.Sign(dragFullDistance.y);
							float rotateValue = rotateSign * Mathf.Min(
								Mathf.Abs(dragFullDistance.y) * m_GameConfig.AnalogRotateSpeed / InputMetrics.InputPrecision,
								0.5f // Rotation is [-0.5, 0.5].
							);

							//Debug.LogWarning($"Drag rotate STARTED with {dragFullDistance.y} ({rotateValue}) at {dragDuration} frame {Time.frameCount}");

							// Offset larger than 0.5f will trigger rotation.
							// If this is a swipe, don't allow more than 1 rotation.
							if (Mathf.Abs(rotateValue + currentRotateOffset) >= 0.5f) {
								m_PointerDragSwiped = true;
								rotateValue += rotateSign * 0.2f; // 0.5 means visually rotation is half-way. Snap it visually towards the end.
								//Debug.LogWarning($"SWIPE INITIAL! SKIP! {rotateValue} for {dragDuration}");
							}

							m_ReplayRecording.AddAndRun(ReplayActionType.OffsetRotate, rotateValue);
						}
					}
				}
			}
		}
	}
}