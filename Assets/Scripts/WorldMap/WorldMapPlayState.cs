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
		private WorldMapController m_LevelController;
		private UI.WorldMapUIController m_UIController;

		private Vector2 m_MovementInput;
		private Vector2 m_MovementCurrent;
		private Vector2 m_MovementVelocity;
		private float m_ZoomInput;
		private float m_ZoomCurrent;
		private float m_ZoomVelocity;

		private bool m_PointerPressed = false;
		private Vector2 m_PointerPressedLastPosition;
		private Vector2 m_DragInertia;

		private InputEnabler m_InputEnabler;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_UIController);

			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.WorldMapPlay);
			m_PlayerControls.WorldMapPlay.SetCallbacks(this);

			// You don't want "Return" key to trigger selected buttons.
			m_InputEnabler.Disable(m_PlayerControls.UI.Submit);
			m_InputEnabler.Disable(m_PlayerControls.UI.Navigate);

			m_UIController.SwitchState(UI.WorldMapUIState.Play);
		}

		public void ExitState()
		{
			m_PlayerControls.WorldMapPlay.SetCallbacks(null);
			m_InputEnabler.Dispose();
		}

		public void OnPointerClick(InputAction.CallbackContext context)
		{
			if (context.phase != InputActionPhase.Performed)
				return;

			if (UnityEngine.EventSystems.EventSystem.current == null)
				return;

			if (UIUtils.RaycastUIElements(Pointer.current.position.ReadValue()).Count > 0)
				return;

			m_LevelController.TryWorldSelect(Pointer.current?.position.ReadValue() ?? new Vector2(Screen.width / 2f, Screen.height / 2f));
		}

		public void OnDiscworldMovement(InputAction.CallbackContext context)
		{
			m_MovementInput = context.ReadValue<Vector2>();
		}

		public void OnDiscworldZoom(InputAction.CallbackContext context)
		{
			m_ZoomInput = context.ReadValue<float>();
		}

		// Pointer (touch or mouse) gesture detections.
		public void OnPointerPress(InputAction.CallbackContext context)
		{
			if (Pointer.current == null)
				return;

			switch (context.phase) {
				case InputActionPhase.Started:
					m_PointerPressedLastPosition = Pointer.current.position.ReadValue();

					// Pressed on the UI.
					if (UIUtils.RaycastUIElements(m_PointerPressedLastPosition).Count > 0)
						return;

					m_PointerPressed = true;
					break;

				case InputActionPhase.Canceled:
				case InputActionPhase.Disabled:
					m_PointerPressed = false;
					break;
			}
		}

		public void Update()
		{
			m_MovementCurrent = Vector2.SmoothDamp(m_MovementCurrent, m_MovementInput, ref m_MovementVelocity, 0.3f, 1f);
			m_ZoomCurrent = Mathf.SmoothDamp(m_ZoomCurrent, m_ZoomInput, ref m_ZoomVelocity, 0.3f);

			m_LevelController.RotateDiscworld(m_MovementCurrent.x);
			m_LevelController.MoveCamera(m_MovementCurrent.y);
			m_LevelController.ZoomCamera(m_ZoomCurrent);

			if (m_DragInertia.sqrMagnitude > 0.05f) {
				// If still dragging, loose inertia faster. Avoid too much inertia on releasing after full stop.
				m_DragInertia = Vector2.Lerp(m_DragInertia, Vector2.zero, m_LevelController.DragDamp * (m_PointerPressed ? 2f : 1f));

				if (!m_PointerPressed) {
					m_LevelController.RotateDiscworld(m_DragInertia.x);
					m_LevelController.MoveCamera(m_DragInertia.y);
				}
			}

			if (m_PointerPressed && Pointer.current != null) {
				Vector2 delta = m_PointerPressedLastPosition - Pointer.current.position.ReadValue();

				m_DragInertia += delta * 0.5f;

				if (delta.magnitude >= 0.1f) {
					// HACK: Animation curve was tweaked to work ok for fullscreen 1920x1200. When resolution or fov is different, apply to curve.
					float zoomMultiplier = m_LevelController.ZoomToDrag.Evaluate(m_LevelController.ZoomNormalized) * 1920f / Screen.width;
					m_LevelController.RotateDiscworld(delta.x * zoomMultiplier);
					m_LevelController.MoveCamera(delta.y * zoomMultiplier);
				}

				m_PointerPressedLastPosition = Pointer.current.position.ReadValue();
			}


		}
	}
}