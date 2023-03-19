using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Use this to forward some UI objects input toward specific player root.
	/// You can set the <see cref="m_PlayerRootObject"/> edit time or <see cref="PlayerRootObject"/> runtime via script.
	/// </summary>
	public class UIPlayerRootForwarder : MonoBehaviour, IPlayerRoot
	{
		// Used for edit time setup only.
		[SerializeField] private UIPlayerRootObject m_PlayerRootObject;

		public IPlayerRoot PlayerRootObject;

		public bool IsActive => PlayerRootObject?.IsActive ?? false;

		public EventSystem EventSystem => PlayerRootObject?.EventSystem;

		public PlayerIndex PlayerIndex => PlayerRootObject?.PlayerIndex ?? PlayerIndex.MasterPlayer;

		public GameObject SelectedGameObject => PlayerRootObject?.SelectedGameObject;

		public void SetSelectedGameObject(GameObject selected) => PlayerRootObject?.SetSelectedGameObject(selected);

		public T GetContextReference<T>() => PlayerRootObject != null ? PlayerRootObject.GetContextReference<T>() : default;

		public UIPlayerRootObject GetRootObject() => PlayerRootObject?.GetRootObject();

		void Awake()
		{
			if (PlayerRootObject == null) {
				PlayerRootObject = m_PlayerRootObject;
			}
		}

		void OnDestroy()
		{
			PlayerRootObject = null;
		}
	}
}