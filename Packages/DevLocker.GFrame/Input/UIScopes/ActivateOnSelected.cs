using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When the <see cref="Selectable"/> on this object is selected, activate the target game object.
	/// Use this to display or hide hotkeys next to buttons when selected.
	/// </summary>
	public class ActivateOnSelected : MonoBehaviour
	{
		public GameObject ActivatedObject;

		private GameObject m_LastSelectedObject;

		void Update()
		{
			if (EventSystem.current == null)
				return;

			if (m_LastSelectedObject != EventSystem.current.currentSelectedGameObject) {
				m_LastSelectedObject = EventSystem.current.currentSelectedGameObject;

				if (m_LastSelectedObject && m_LastSelectedObject.transform.IsChildOf(transform)) {
					ActivatedObject.SetActive(true);
				} else {
					ActivatedObject.SetActive(false);
				}
			}
		}

		void OnValidate()
		{
			if (ActivatedObject == null) {
				Debug.LogError($"{name} has missing {nameof(ActivatedObject)}...", this);
			}
		}
	}
}