#if USE_INPUT_SYSTEM

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Put next to or under a UI.Selectable to be selected on performing the specified input action.
	/// Note that this action has to be enabled in order to be invoked.
	/// </summary>
	public class HotkeySelectScopeElement : HotkeyBaseScopeElement
	{
		private Selectable m_Selectable;

		protected override void OnInvoke()
		{
			if (m_Selectable == null) {
				m_Selectable = GetComponentInParent<Selectable>();
			}

			EventSystem.current.SetSelectedGameObject(gameObject);
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			// OnValidate() gets called even if object is not active.
			// HACK: Because Unity are idiots and missed this overload.
			//var selectable = GetComponentInParent<Selectable>(true);

			Selectable selectable = null;
			var tr = transform;
			while (tr) {
				selectable = tr.GetComponent<Selectable>();
				if (selectable)
					break;

				tr = tr.parent;
			}


			if (selectable == null) {
				Debug.LogError($"No valid selectable was found for HotkeySelect {name}", this);
				return;
			}
		}
	}
}

#endif