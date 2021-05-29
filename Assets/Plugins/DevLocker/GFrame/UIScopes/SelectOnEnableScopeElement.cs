using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.UIScope
{
	/// <summary>
	/// When this scope element is enabled, it will set this object as selected in the Unity event system.
	/// </summary>
	public class SelectOnEnableScopeElement : MonoBehaviour, IScopeElement
	{
		[Tooltip("Start by selecting current object, but remember what the selection was on disable.\nOn re-enabling, resume from that selection.")]
		public bool PersistentSelection = false;

		private GameObject m_PersistedSelection = null;
		private bool m_SelectRequested = false;

		void OnEnable()
		{
			m_SelectRequested = true;
		}

		void Update()
		{
			if (m_SelectRequested && EventSystem.current) {

#if USE_INPUT_SYSTEM
				// Hotkeys subscribe for the InputAction.perform event, which executes on key press / down,
				// while "Submit" action of the InputSystemUIInputModule runs on key release / up.
				// This makes hotkey being executed, new screen scope shown and executing the newly selected button on release.
				// We don't want that so wait till submit action is no more pressed.
				var inputModule = EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
				var submitAction = inputModule?.submit?.action;
				if (submitAction != null && submitAction.IsPressed())
					return;
#endif

				m_SelectRequested = false;
				if (m_PersistedSelection && m_PersistedSelection.activeInHierarchy) {
					EventSystem.current.SetSelectedGameObject(m_PersistedSelection);
				} else {
					EventSystem.current.SetSelectedGameObject(gameObject);
				}
			}
		}

		void OnDisable()
		{
			if (EventSystem.current && PersistentSelection) {
				m_PersistedSelection = EventSystem.current.currentSelectedGameObject;
			}
		}
	}
}