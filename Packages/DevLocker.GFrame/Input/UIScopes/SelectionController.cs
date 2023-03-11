using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When this component is enabled, it will set this object as selected in the Unity event system.
	/// </summary>
	public class SelectionController : MonoBehaviour
	{
		public enum NoSelectionActionType
		{
			DoNothing = 0,
			SelectStartObject = 4,
			SelectLastSelectedObject = 8,
		};

		[Tooltip("Select this object on enable.")]
		public Selectable StartSelection;

		[Tooltip("Initially select the starting object, but remember what the selection was on disable.\nOn re-enabling, resume from that selection.")]
		public bool PersistentSelection = false;

		[Tooltip("What should happen if no object is selected (e.g. user mouse-clicks on empty space and selection is lost)? Useful with FilterControlScheme.")]
		public NoSelectionActionType NoSelectionAction = NoSelectionActionType.SelectLastSelectedObject;

		[InputControlSchemePicker]
		[Tooltip("Control schemes to update for. One control scheme can match multiple devices (e.g. XBox and PS gamepads). Use the picker to avoid typos.")]
		public string[] FilterControlScheme;

		[Tooltip("Set selected object to none if the active control scheme doesn't match the filter. E.g. hide selection for mouse & keyboard, but show it for Gamepad.\nIgnored if no filter is specified.")]
		public bool RemoveSelectionOnControlSchemeMismatch = true;

		private GameObject m_PersistedSelection = null;
		private bool m_SelectRequested = false;
		private string m_LastControlScheme = "";
		private bool m_ControlSchemeMatched = true;

		private static SelectionController m_ActiveInstance;

		// Used for multiple event systems (e.g. split screen).
		protected UIEventSystemRootObject.EventSystemLocator m_ESLocator;

		protected virtual void Start()
		{
			m_ESLocator = new UIEventSystemRootObject.EventSystemLocator(gameObject);
		}

		protected virtual void OnEnable()
		{
			m_SelectRequested = true;
		}

		void Update()
		{
			if (m_ESLocator.EventSystem == null)
				return;

			if (FilterControlScheme.Length != 0 && InputContextManager.InputContext != null) {
				UpdateControlScheme();
			}

			if (m_SelectRequested) {

				// Call this on update, to avoid errors while switching active object (turn on one, turn off another).
				if (m_ActiveInstance == null) {
					m_ActiveInstance = this;
				} else {
					Debug.LogError($"There are two or more {nameof(SelectionController)} instances active at the same time - this is not allowed. Currently active: \"{m_ActiveInstance.name}\". Additional instance: \"{name}\"", this);
				}

#if USE_INPUT_SYSTEM
				// Hotkeys subscribe for the InputAction.perform event, which executes on key press / down,
				// while "Submit" action of the InputSystemUIInputModule runs on key release / up.
				// This makes hotkey being executed, new screen scope shown and executing the newly selected button on release.
				// We don't want that so wait till submit action is no more pressed.
				var inputModule = m_ESLocator.EventSystem.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
				var submitAction = inputModule?.submit?.action;
				if (submitAction != null && submitAction.IsPressed())
					return;
#endif

				m_SelectRequested = false;

				GameObject targetSelection = (PersistentSelection && m_PersistedSelection && m_PersistedSelection.activeInHierarchy)
					? m_PersistedSelection
					: (StartSelection ? StartSelection.gameObject : null)
					;

				if (targetSelection && !targetSelection.activeInHierarchy) {
					Debug.LogWarning($"{name} {nameof(SelectionController)} is trying to select inactive object!", this);
				}

				if (m_ControlSchemeMatched) {

					// If UI was deactivated but selection didn't change, activating it back will leave the button selected but not highlighted.
					if (m_ESLocator.SelectedObject == targetSelection) {
						m_ESLocator.SetSelectedObject(null);
					}

					m_ESLocator.SetSelectedObject(targetSelection);

					m_PersistedSelection = m_ESLocator.SelectedObject;

					OnSelected();

				} else {

					m_PersistedSelection = targetSelection;
				}

			} else {

				if (m_ControlSchemeMatched) {
					if (m_ESLocator.SelectedObject) {
						m_PersistedSelection = m_ESLocator.SelectedObject;
					} else {
						DoNoSelectionAction();
					}
				} else {
					if (RemoveSelectionOnControlSchemeMismatch && m_ESLocator.SelectedObject) {
						m_ESLocator.SetSelectedObject(null);
					}
				}
			}
		}

		private void UpdateControlScheme()
		{
			InputControlScheme scheme = InputContextManager.InputContext.GetLastUsedInputControlScheme(PlayerIndex.AnyPlayer);

			if (!m_LastControlScheme.Equals(scheme.bindingGroup)) {

				m_ControlSchemeMatched = false;
				foreach (string filterScheme in FilterControlScheme) {
					if (filterScheme.Equals(scheme.bindingGroup, System.StringComparison.OrdinalIgnoreCase)) {
						m_ControlSchemeMatched = true;
						break;
					}
				}

				m_LastControlScheme = scheme.bindingGroup;
			}
		}

		private void DoNoSelectionAction()
		{
			switch (NoSelectionAction) {

				case NoSelectionActionType.SelectLastSelectedObject:
					OnSelected();
					m_ESLocator.SetSelectedObject(m_PersistedSelection);
					break;

				case NoSelectionActionType.SelectStartObject:
					OnSelected();
					m_ESLocator.SetSelectedObject(StartSelection.gameObject);
					break;

				case NoSelectionActionType.DoNothing:
					break;

				default:
					throw new System.NotSupportedException(NoSelectionAction.ToString());
			}
		}

		protected virtual void OnSelected() { }

		protected virtual void OnDisable()
		{
			if (m_ActiveInstance == this) {
				m_ActiveInstance = null;
			}
		}

		protected virtual void OnValidate()
		{
			if (StartSelection == null
#if UNITY_EDITOR
				&& (Application.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer)
#else
				&& Application.isPlaying
#endif
				) {
				Debug.LogError($"{name} {nameof(SelectionController)} has no start selection set. Disabling!", this);
				enabled = false;
			}
		}
	}
}