using UnityEngine;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When UI selectable is selected under this component target UIScope is forced to be focused.
	/// With it, hotkey icons etc. may pop up.
	/// </summary>
	public class FocusUIScopeOnSelection : MonoBehaviour
	{
		public UIScope Scope;

		private GameObject m_LastSelectedObject;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		void Awake()
		{
			if (Scope == null) {
				Scope = GetComponent<UIScope>();
			}

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		/// <summary>
		/// LateUpdate() as most scripts will do selection on Update() - wait for all of them to finish... hopefully.
		/// <see cref="SelectionController"/>
		/// </summary>
		void LateUpdate()
		{
			if (!m_PlayerContext.IsActive)
				return;

			if (m_LastSelectedObject != m_PlayerContext.SelectedGameObject) {
				m_LastSelectedObject = m_PlayerContext.SelectedGameObject;

				if (!Scope.IsActive && m_LastSelectedObject && m_LastSelectedObject.transform.IsChildOf(transform)) {
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