using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// When using multiple event systems (e.g. split-screen) add this component to the root UI canvas object of each player.
	/// At some point set the <see cref="m_OwnerEventSystem"/> (edit time or via code).
	/// Other components may use this information to work properly in such environment.
	///
	/// Note: this is how Unity UI can support multiple players each having their own UI selection and navigation.
	///		  Basically they are limited by the <see cref="UnityEngine.InputSystem.UI.MultiplayerEventSystem.playerRoot"/>
	///		  Good explanation: https://www.youtube.com/watch?v=Ur2tBl58YOc
	/// </summary>
	public class UIEventSystemRootObject : MonoBehaviour
	{
		public EventSystem OwnerEventSystem => m_OwnerEventSystem;
		[SerializeField] private EventSystem m_OwnerEventSystem;

		/// <summary>
		/// Use this to fetch your owner event system. Simple wrapper.
		/// Helps you avoid some boiler plate code.
		/// </summary>
		public class EventSystemLocator
		{
			// Used for multiple event systems (e.g. split screen).
			private UIEventSystemRootObject m_RootObject;
			public EventSystem EventSystem => m_RootObject ? m_RootObject.OwnerEventSystem : EventSystem.current; // Don't use ??.

			public GameObject SelectedObject => EventSystem?.currentSelectedGameObject;

			public void SetSelectedObject(GameObject selected) => EventSystem?.SetSelectedGameObject(selected);

			/// <summary>
			/// Best place to create your instance is in Start() before any usages. Avoid Awake().
			/// </summary>
			/// <param name="targetObject">The object in question</param>
			public EventSystemLocator(GameObject targetObject)
			{
				/// Do this on start, in case <see cref="UIEventSystemRootObject"/> is added dynamically by code.
				m_RootObject = targetObject.transform.GetComponentInParent<UIEventSystemRootObject>(true);
			}
		}

		public void SetOwnerEventSystem(EventSystem eventSystem)
		{
			m_OwnerEventSystem = eventSystem;
		}

	}
}