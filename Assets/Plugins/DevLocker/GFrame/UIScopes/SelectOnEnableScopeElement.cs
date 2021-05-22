using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.UIScope
{
	/// <summary>
	/// When this scope element is enabled, it will set this object as selected in the Unity event system.
	/// </summary>
	public class SelectOnEnableScopeElement : MonoBehaviour, IScopeElement
	{
		void OnEnable()
		{
			if (EventSystem.current) {
				EventSystem.current.SetSelectedGameObject(gameObject);
			}
		}
	}
}