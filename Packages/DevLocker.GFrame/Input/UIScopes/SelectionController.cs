using DevLocker.GFrame.Input.Contexts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When this component is enabled, it will set this object as selected in the Unity event system.
	/// </summary>
	public class SelectionController : MonoBehaviour
	{
		public enum PersistSelectionActionType
		{
			DoNotPersist = 0,
			AlwaysPersist = 1,
			PersistOnlyIfObjectIsActiveInHierarchy = 4,
		};

		public enum NoSelectionActionType
		{
			DoNothing = 0,
			SelectStartObject = 4,
			SelectLastSelectedObject = 8,
		};

		public enum StartSelectionSourceTypes
		{
			Selectables = 0,
			NavigationGroups = 4,
		}

		public StartSelectionSourceTypes StartSelectionSource = StartSelectionSourceTypes.Selectables;

		[Tooltip("Select the first interactable object from the managed selectables of this navigation group.")]
		public UINavigationGroup[] StartNavigationGroups;

		[Tooltip("Select the first interactable object on enable.")]
		public Selectable[] StartSelections;

		[Tooltip("Persist selection even if component or object is disabled.")]
		public PersistSelectionActionType PersistentSelection = PersistSelectionActionType.PersistOnlyIfObjectIsActiveInHierarchy;

		[Tooltip("What should happen if no object is selected (e.g. user mouse-clicks on empty space and selection is lost)? Useful when device does not support UI navigation.")]
		public NoSelectionActionType NoSelectionAction = NoSelectionActionType.SelectLastSelectedObject;

		[Tooltip("Should it consider only selections that are child of this component for persistent or last selected object?")]
		public bool TrackOnlyChildren = true;

		[Tooltip("Set selected object to none if the current device doesn't support it. E.g. hide selection for mouse & keyboard, but show it for Gamepad.\nCheck the device InputBindingDisplayAsset.")]
		[FormerlySerializedAs("RemoveSelectionOnControlSchemeMismatch")]
		public bool RemoveSelectionIfDeviceDoesntSupportIt = true;

		public bool ClearSelectionOnDisable = true;

		private GameObject m_PersistedSelection = null;
		private Selectable m_PersistedSelectable;	// Not sure if having selected object without selectable component is possible.
		private bool m_PersistedIsAvailable => m_PersistedSelectable
			? m_PersistedSelectable.IsInteractable() && m_PersistedSelectable.isActiveAndEnabled
			: m_PersistedSelection != null && m_PersistedSelection.activeInHierarchy
		;

		private bool m_SelectRequested = false;
		private bool m_ControlSchemeMatched = true;

		private static Dictionary<PlayerContextUIRootObject, SelectionController> s_ActiveInstances = new Dictionary<PlayerContextUIRootObject, SelectionController>();

		private List<CanvasGroup> m_CanvasGroups = new List<CanvasGroup>();

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		/// <summary>
		/// Find the first active and interactable selectable from <see cref="StartNavigationGroups"/> or <see cref="StartSelections"/>.
		/// </summary>
		public virtual Selectable GetStartSelection()
		{
			switch(StartSelectionSource) {
				case StartSelectionSourceTypes.Selectables:
					if (StartSelections == null)
						return null;

					foreach(Selectable selectable in StartSelections) {
						if (selectable && selectable.isActiveAndEnabled && selectable.IsInteractable())
							return selectable;
					}
					break;

				case StartSelectionSourceTypes.NavigationGroups:
					if (StartNavigationGroups == null)
						return null;

					foreach (UINavigationGroup navigationGroup in StartNavigationGroups) {

						if (navigationGroup.FirstSelectable)
							return navigationGroup.FirstSelectable;

						foreach (Selectable selectable in navigationGroup.ManagedSelectables) {
							if (selectable && selectable.isActiveAndEnabled && selectable.IsInteractable())
								return selectable;
						}
					}
					break;
			}


			return null;
		}

		public virtual bool IsInStartSelection(Selectable selectable)
		{
			switch(StartSelectionSource) {
				case StartSelectionSourceTypes.Selectables:
					return StartSelections != null && System.Array.IndexOf(StartSelections, selectable) != -1;

				case StartSelectionSourceTypes.NavigationGroups:
					if (StartNavigationGroups == null)
						return false;

					foreach (UINavigationGroup navigationGroup in StartNavigationGroups) {
						foreach (Selectable startSelectable in navigationGroup.ManagedSelectables) {
							if (startSelectable == selectable)
								return true;
						}
					}
					break;
			}

			return false;
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

			if (PersistentSelection == PersistSelectionActionType.PersistOnlyIfObjectIsActiveInHierarchy && !gameObject.activeInHierarchy) {
				m_PersistedSelection = null;
				m_PersistedSelectable = null;
			}
		}

		void Update()
		{
			if (!m_PlayerContext.IsActive)
				return;

			if (m_PlayerContext.InputContext != null) {
				m_ControlSchemeMatched = m_PlayerContext.InputContext.DeviceSupportsUINavigationSelection;
			}

			// Wait till clickable to work with selection.
			if (!IsClickable()) {
				if (!m_ControlSchemeMatched && RemoveSelectionIfDeviceDoesntSupportIt && m_PlayerContext.SelectedGameObject) {
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

				GameObject targetSelection = (PersistentSelection > 0 && m_PersistedIsAvailable)
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
							|| selectable && IsInStartSelection(selectable)
							|| m_PlayerContext.SelectedGameObject.transform.IsChildOf(transform)
							) {
							m_PersistedSelection = m_PlayerContext.SelectedGameObject;
							m_PersistedSelectable = m_PersistedSelection ? m_PersistedSelection.GetComponent<Selectable>() : null;
						}

					} else {
						DoNoSelectionAction();
					}
				} else {
					if (RemoveSelectionIfDeviceDoesntSupportIt && m_PlayerContext.SelectedGameObject) {
						m_PlayerContext.SetSelectedGameObject(null);
					}
				}
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
			switch(StartSelectionSource) {
				case StartSelectionSourceTypes.Selectables:
					if (StartSelections != null) {
						// Having no start selection is valid if selectables will be created dynamically.
						foreach (Selectable selectable in StartSelections) {
							if (selectable == null) {
								Debug.LogError($"[Input] {name} {nameof(SelectionController)} has missing start selection.", this);
							}
						}
					}
					break;

				case StartSelectionSourceTypes.NavigationGroups:
					if (StartNavigationGroups != null) {
						// Having no start selection is valid if selectables will be created dynamically.
						foreach (UINavigationGroup navGroup in StartNavigationGroups) {
							if (navGroup == null) {
								Debug.LogError($"[Input] {name} {nameof(SelectionController)} has missing start selection.", this);
							}
						}
					}
					break;
			}

		}
	}


#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(SelectionController), true)]
	[UnityEditor.CanEditMultipleObjects]
	internal class SelectionControllerEditor : UnityEditor.Editor
	{
		private static SelectionController.StartSelectionSourceTypes[] s_StartSelectionSourceTypeValues = (SelectionController.StartSelectionSourceTypes[]) System.Enum.GetValues(typeof(SelectionController.StartSelectionSourceTypes));

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			var startSelectionSource = serializedObject.FindProperty(nameof(SelectionController.StartSelectionSource));
			UnityEditor.EditorGUILayout.PropertyField(startSelectionSource);

			var sourceType = s_StartSelectionSourceTypeValues[startSelectionSource.enumValueIndex];

			switch(sourceType) {
				case SelectionController.StartSelectionSourceTypes.Selectables:
					UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.StartSelections)));
					break;
				case SelectionController.StartSelectionSourceTypes.NavigationGroups:
					UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.StartNavigationGroups)));
					break;
				default:
					Debug.LogError($"Unknown type {sourceType}", target);
					break;
			}

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.PersistentSelection)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.NoSelectionAction)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.TrackOnlyChildren)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.RemoveSelectionIfDeviceDoesntSupportIt)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(SelectionController.ClearSelectionOnDisable)));

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}