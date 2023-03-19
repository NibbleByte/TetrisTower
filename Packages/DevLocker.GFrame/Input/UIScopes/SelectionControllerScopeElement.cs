using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When this component is enabled by the UIScope (when focused), it will set this object as selected in the Unity event system.
	/// </summary>
	public class SelectionControllerScopeElement : SelectionController, IScopeElement
	{
		public bool ClearSelectionOnDisable = false;

		protected override void OnDisable()
		{
			base.OnDisable();

			if (ClearSelectionOnDisable && m_PlayerUI.IsActive) {
				m_PlayerUI.SetSelectedGameObject(null);
			}
		}
	}
}