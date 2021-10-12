using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When UI selectable is selected under this component target UIScope is force activated.
	/// With it, hotkey icons etc. may pop up.
	/// </summary>
	public class ActivateUIScopeOnSelection : MonoBehaviour
	{
		public UIScope Scope;

		private GameObject m_LastSelectedObject;

		void Awake()
		{
			if (Scope == null) {
				Scope = GetComponent<UIScope>();
			}
		}

		void Update()
		{
			if (EventSystem.current == null)
				return;

			if (m_LastSelectedObject != EventSystem.current.currentSelectedGameObject) {
				m_LastSelectedObject = EventSystem.current.currentSelectedGameObject;

				if (!UIScope.IsScopeActive(Scope) && m_LastSelectedObject && m_LastSelectedObject.transform.IsChildOf(transform)) {
					Scope.ForceRefocusScope();
				}
			}
		}

		private void OnDisable()
		{
			m_LastSelectedObject = null;
		}

		private void OnValidate()
		{
			Utils.Validation.ValidateMissingObject(this, Scope, nameof(Scope));
		}
	}
}