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

		[Tooltip("Select the first interactable object on enable.")]
		public Selectable[] StartSelections;

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

		public bool ClearSelectionOnDisable = true;

		private GameObject m_PersistedSelection = null;
		private Selectable m_PersistedSelectable;	// Not sure if having selected object without selectable component is possible.
		private bool m_PersistedIsAvailable => m_PersistedSelectable
			? m_PersistedSelectable.IsInteractable() && m_PersistedSelectable.isActiveAndEnabled
			: m_PersistedSelection != null && m_PersistedSelection.activeInHierarchy
		;

		private bool m_SelectRequested = false;
		private string m_LastControlScheme = "";
		private bool m_ControlSchemeMatched = true;

		private static Dictionary<PlayerContextUIRootObject, SelectionController> s_ActiveInstances = new Dictionary<PlayerContextUIRootObject, SelectionController>();

		private List<CanvasGroup> m_CanvasGroups = new List<CanvasGroup>();

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		/// <summary>
		/// Find the first active and interactable selectable from <see cref="StartSelections"/>.
		/// </summary>
		public virtual Selectable GetStartSelection()
		{
			if (StartSelections == null)
				return null;

			foreach(Selectable selectable in StartSelections) {
				if (selectable && selectable.isActiveAndEnabled && selectable.IsInteractable())
					return selectable;
			}

			return null;
		}

		/// <summary>
		/// Get the active instance for the specified player context (i.e. singleton).
		/// </summary>
		public static SelectionController GetActiveInstanceFor(PlayerContextUIRootObject playerContext)
		{
			s_ActiveInstances.TryGetValue(playerContext, out SelectionController instance);

			return instance;
		}

		protected virtual void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		protected virtual void OnEnable()
		{
			m_SelectRequested = true;

			CollectParentCanvasGroups();
		}

		protected virtual void OnDisable()
		{
			if (GetActiveInstanceForThisPlayer() == this) {
				s_ActiveInstances.Remove(m_PlayerContext.GetRootObject());
			}

			if (ClearSelectionOnDisable && m_PlayerContext.IsActive) {
				m_PlayerContext.SetSelectedGameObject(null);
			}
		}

		void Update()
		{
			if (!m_PlayerContext.IsActive)
				return;

			if (FilterControlScheme.Length != 0 && m_PlayerContext.InputContext != null) {
				UpdateControlScheme();
			}

			// Wait till clickable to work with selection.
			if (!IsClickable()) {
				if (!m_ControlSchemeMatched && RemoveSelectionOnControlSchemeMismatch && m_PlayerContext.SelectedGameObject) {
					m_PlayerContext.SetSelectedGameObject(null);
				}

				if (ClearSelectionOnDisable && m_PlayerContext.SelectedGameObject) {
					m_PlayerContext.SetSelectedGameObject(null);
				}

				return;
			}

			if (m_SelectRequested) {

				SelectionController activeInstance = GetActiveInstanceForThisPlayer();

				// Call this on update, to avoid errors while switching active object (turn on one, turn off another).
				if (activeInstance == null) {
					s_ActiveInstances.Add(m_PlayerContext.GetRootObject(), this);
				} else {
					Debug.LogError($"[Input] There are two or more {nameof(SelectionController)} instances active at the same time - this is not allowed. Currently active: \"{activeInstance.name}\". Additional instance: \"{name}\"", this);
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

				GameObject targetSelection = (PersistentSelection && m_PersistedIsAvailable)
					? m_PersistedSelection
					: GetStartSelection()?.gameObject // Null-check is safe.
					;

				if (targetSelection && !targetSelection.activeInHierarchy) {
					Debug.LogWarning($"[Input] {name} {nameof(SelectionController)} is trying to select inactive object!", this);
				}

				if (m_ControlSchemeMatched) {

					// If UI was deactivated but selection didn't change, activating it back will leave the button selected but not highlighted.
					if (m_PlayerContext.SelectedGameObject == targetSelection) {
						m_PlayerContext.SetSelectedGameObject(null);
					}

					m_PlayerContext.SetSelectedGameObject(targetSelection);

					m_PersistedSelection = m_PlayerContext.SelectedGameObject;
					m_PersistedSelectable = m_PersistedSelection ? m_PersistedSelection.GetComponent<Selectable>() : null;

					OnSelected();

				} else {

					m_PersistedSelection = targetSelection;
					m_PersistedSelectable = m_PersistedSelection ? m_PersistedSelection.GetComponent<Selectable>() : null;
				}

			} else {

				if (m_ControlSchemeMatched) {
					GameObject selectedObject = m_PlayerContext.SelectedGameObject;
					Selectable selectable = selectedObject ? selectedObject.GetComponent<Selectable>() : null;
					if (selectedObject && selectedObject.activeInHierarchy && (selectable == null || selectable.IsInteractable())) {

						if (!TrackOnlyChildren
							|| selectable && System.Array.IndexOf(StartSelections, selectable) != -1
							|| m_PlayerContext.SelectedGameObject.transform.IsChildOf(transform)
							) {
							m_PersistedSelection = m_PlayerContext.SelectedGameObject;
							m_PersistedSelectable = m_PersistedSelection ? m_PersistedSelection.GetComponent<Selectable>() : null;
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
			GameObject startObject = GetStartSelection()?.gameObject; // Null-check is safe.

			switch (NoSelectionAction) {

				case NoSelectionActionType.SelectLastSelectedObject:
					m_PlayerContext.SetSelectedGameObject(m_PersistedIsAvailable ? m_PersistedSelection : startObject);
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

		protected SelectionController GetActiveInstanceForThisPlayer()
		{
			PlayerContextUIRootObject rootObject = m_PlayerContext?.GetRootObject();
			if (rootObject == null)
				return null;

			SelectionController result;
			s_ActiveInstances.TryGetValue(rootObject, out result);

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
			if (StartSelections != null) {
				// Having no start selection is valid if selectables will be created dynamically.
				foreach (Selectable selectable in StartSelections) {
					if (selectable == null) {
						Debug.LogError($"[Input] {name} {nameof(SelectionController)} has missing start selection.", this);
					}
				}
			}
		}
	}
}