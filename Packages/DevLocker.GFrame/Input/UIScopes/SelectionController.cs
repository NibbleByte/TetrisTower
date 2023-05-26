using DevLocker.GFrame.Input.Contexts;
using System.Collections.Generic;
using UnityEngine;
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

		[Tooltip("Should it consider only selections that are child of this component for persistent or last selected object?")]
		public bool TrackOnlyChildren = true;

		[InputControlSchemePicker]
		[Tooltip("Control schemes to update for. One control scheme can match multiple devices (e.g. XBox and PS gamepads). Use the picker to avoid typos.")]
		public string[] FilterControlScheme;

		[Tooltip("Set selected object to none if the active control scheme doesn't match the filter. E.g. hide selection for mouse & keyboard, but show it for Gamepad.\nIgnored if no filter is specified.")]
		public bool RemoveSelectionOnControlSchemeMismatch = true;

		private GameObject m_PersistedSelection = null;
		private bool m_SelectRequested = false;
		private string m_LastControlScheme = "";
		private bool m_ControlSchemeMatched = true;

		private static Dictionary<PlayerContextUIRootObject, SelectionController> m_ActiveInstances = new Dictionary<PlayerContextUIRootObject, SelectionController>();

		private List<CanvasGroup> m_CanvasGroups = new List<CanvasGroup>();

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected virtual void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		protected virtual void OnEnable()
		{
			m_SelectRequested = true;

			CollectParentCanvasGroups();
		}

		void Update()
		{
			if (!m_PlayerContext.IsActive)
				return;

			if (FilterControlScheme.Length != 0 && m_PlayerContext.InputContext != null) {
				UpdateControlScheme();
			}

			// Wait till clickable to work with selection.
			if (!IsClickable())
				return;

			if (m_SelectRequested) {

				SelectionController activeInstance = GetActiveInstanceForThisPlayer();

				// Call this on update, to avoid errors while switching active object (turn on one, turn off another).
				if (activeInstance == null) {
					m_ActiveInstances.Add(m_PlayerContext.GetRootObject(), this);
				} else {
					Debug.LogError($"There are two or more {nameof(SelectionController)} instances active at the same time - this is not allowed. Currently active: \"{activeInstance.name}\". Additional instance: \"{name}\"", this);
				}

#if USE_INPUT_SYSTEM
				// Hotkeys subscribe for the InputAction.perform event, which executes on key press / down,
				// while "Submit" action of the InputSystemUIInputModule runs on key release / up.
				// This makes hotkey being executed, new screen scope shown and executing the newly selected button on release.
				// We don't want that so wait till submit action is no more pressed.
				var inputModule = m_PlayerContext.EventSystem.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
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
					if (m_PlayerContext.SelectedGameObject == targetSelection) {
						m_PlayerContext.SetSelectedGameObject(null);
					}

					m_PlayerContext.SetSelectedGameObject(targetSelection);

					m_PersistedSelection = m_PlayerContext.SelectedGameObject;

					OnSelected();

				} else {

					m_PersistedSelection = targetSelection;
				}

			} else {

				if (m_ControlSchemeMatched) {
					if (m_PlayerContext.SelectedGameObject && m_PlayerContext.SelectedGameObject.activeInHierarchy) {

						if (!TrackOnlyChildren || m_PlayerContext.SelectedGameObject.transform.IsChildOf(transform)) {
							m_PersistedSelection = m_PlayerContext.SelectedGameObject;
						}

					} else {
						DoNoSelectionAction();
					}
				} else {
					if (RemoveSelectionOnControlSchemeMismatch && m_PlayerContext.SelectedGameObject) {
						m_PlayerContext.SetSelectedGameObject(null);
					}
				}
			}
		}

		private void UpdateControlScheme()
		{
			InputControlScheme scheme = m_PlayerContext.InputContext.GetLastUsedInputControlScheme();

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
			GameObject startObject = StartSelection ? StartSelection.gameObject : null;
			
			switch (NoSelectionAction) {

				case NoSelectionActionType.SelectLastSelectedObject:
					m_PlayerContext.SetSelectedGameObject(m_PersistedSelection && m_PersistedSelection.activeInHierarchy ? m_PersistedSelection : startObject);
					OnSelected();
					break;

				case NoSelectionActionType.SelectStartObject:
					m_PlayerContext.SetSelectedGameObject(startObject);
					OnSelected();
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
			if (GetActiveInstanceForThisPlayer() == this) {
				m_ActiveInstances.Remove(m_PlayerContext.GetRootObject());
			}
		}

		protected SelectionController GetActiveInstanceForThisPlayer()
		{
			PlayerContextUIRootObject rootObject = m_PlayerContext?.GetRootObject();
			if (rootObject == null)
				return null;

			SelectionController result;
			m_ActiveInstances.TryGetValue(rootObject, out result);

			return result;
		}

		private void CollectParentCanvasGroups()
		{
			m_CanvasGroups.Clear();

			var group = gameObject.GetComponentInParent<CanvasGroup>(true);

			while (group) {
				m_CanvasGroups.Add(group);

				if (group.ignoreParentGroups)
					break;

				Transform parent = group.transform.parent;
				if (parent == null)
					break;

				group = group.transform.parent.GetComponentInParent<CanvasGroup>(true);
			}
		}

		private bool IsClickable()
		{
			foreach(CanvasGroup group in m_CanvasGroups) {
				if (group == null || !group.enabled)
					continue;

				if (!group.interactable || !group.blocksRaycasts)
					return false;
			}

			return true;
		}

		protected virtual void OnValidate()
		{
			/* Having no start selection is valid if selectables will be created dynamically.
			if (StartSelection == null
#if UNITY_EDITOR
				&& (Application.isPlaying || UnityEditor.BuildPipeline.isBuildingPlayer)
#else
				&& Application.isPlaying
#endif
				) {
				Debug.LogError($"{name} {nameof(SelectionController)} has no start selection set. Disabling!", this);
				enabled = false;
			}*/
		}
	}
}