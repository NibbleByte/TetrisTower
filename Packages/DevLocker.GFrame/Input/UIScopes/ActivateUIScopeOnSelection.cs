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

		// Used for multiple event systems (e.g. split screen).
		protected UIPlayerRootObject m_PlayerUI;

		void Awake()
		{
			if (Scope == null) {
				Scope = GetComponent<UIScope>();
			}

			m_PlayerUI = UIPlayerRootObject.GetPlayerUIRootFor(gameObject);
		}

		void Update()
		{
			if (m_PlayerUI.EventSystem == null)
				return;

			if (m_LastSelectedObject != m_PlayerUI.SelectedGameObject) {
				m_LastSelectedObject = m_PlayerUI.SelectedGameObject;

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