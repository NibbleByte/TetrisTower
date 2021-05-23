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

		void OnEnable()
		{
			if (EventSystem.current) {
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