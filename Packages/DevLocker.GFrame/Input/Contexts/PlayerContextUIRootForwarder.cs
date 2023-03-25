using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.Contexts
{
	/// <summary>
	/// Use this to forward some UI objects input toward specific <see cref="PlayerContextUIRootObject"/>.
	/// You can set the <see cref="m_PlayerRootObject"/> edit time or <see cref="PlayerRootObject"/> runtime via script.
	/// </summary>
	public class PlayerContextUIRootForwarder : MonoBehaviour, IPlayerContext
	{
		// Used for edit time setup only.
		[SerializeField] private PlayerContextUIRootObject m_PlayerRootObject;

		public IPlayerContext PlayerRootObject { get; private set; }

		public bool IsActive => PlayerRootObject?.IsActive ?? false;

		public string PlayerName => PlayerRootObject?.PlayerName ?? "";

		public IInputContext InputContext => PlayerRootObject?.InputContext;

		public EventSystem EventSystem => PlayerRootObject?.EventSystem;

		public GameObject SelectedGameObject => PlayerRootObject?.SelectedGameObject;

		public void SetSelectedGameObject(GameObject selected) => PlayerRootObject?.SetSelectedGameObject(selected);

		public T GetContextReference<T>() => PlayerRootObject != null ? PlayerRootObject.GetContextReference<T>() : default;

		public PlayerContextUIRootObject GetRootObject() => PlayerRootObject?.GetRootObject();

		private IPlayerContext.SetupCallbackDelegate m_CallbacksOnSetup;

		public void AddSetupCallback(IPlayerContext.SetupCallbackDelegate setupReadyCallback)
		{
			if (PlayerRootObject != null) {
				PlayerRootObject.AddSetupCallback(m_CallbacksOnSetup);
			} else {
				m_CallbacksOnSetup += setupReadyCallback;
			}
		}

		/// <summary>
		/// Use this to setup the actual <see cref="PlayerContextUIRootObject"/> this instance forwards to.
		/// </summary>
		public void SetupTargetRootObject(PlayerContextUIRootObject rootObject)
		{
			PlayerRootObject = rootObject;

			PlayerRootObject.AddSetupCallback(m_CallbacksOnSetup);
			m_CallbacksOnSetup = null;
		}

		void Awake()
		{
			if (PlayerRootObject == null && m_PlayerRootObject) {
				SetupTargetRootObject(m_PlayerRootObject);
			}
		}

		void OnDestroy()
		{
			PlayerRootObject = null;
		}

	}
}