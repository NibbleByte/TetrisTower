using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Abstract interface for <see cref="UIPlayerRootObject"/> and <see cref="UIPlayerRootForwarder"/>.
	/// Use it as reference for the player root. Remember, you have a Global player root as well - <see cref="UIPlayerRootObject"/>
	/// </summary>
	public interface IPlayerRoot
	{
		/// <summary>
		/// Is the player setup ready.
		/// </summary>
		public bool IsActive { get; }

		/// <summary>
		/// Event system used by this player.
		/// </summary>
		EventSystem EventSystem { get; }

		/// <summary>
		/// Player Index of the owner player.
		/// </summary>
		PlayerIndex PlayerIndex { get; }

		/// <summary>
		/// Short-cut - get selected UI object for this player.
		/// </summary>
		GameObject SelectedGameObject { get; }

		/// <summary>
		/// Short-cut - set selected UI object for this player.
		/// </summary>
		void SetSelectedGameObject(GameObject selected);

		/// <summary>
		/// Get arbitrary object from this player root. Useful for attaching level state stack or similar per player.
		/// </summary>
		T GetContextReference<T>();

		/// <summary>
		/// Get the top-most root object.
		/// </summary>
		UIPlayerRootObject GetRootObject();
	}

	/// <summary>
	/// When using multiple event systems (e.g. split-screen) add this component to the root UI canvas object of each player.
	/// This marks all children as owned by the specific player.
	/// At some point set the owning event system via <see cref="SetupPlayer(EventSystem, PlayerIndex)"/> (edit time or via code).
	/// Other child components may use this information to work properly in such environment.
	///
	/// Make sure the component exists on the Awake() step of the child objects, i.e. have it defined in the UI prefab. If that is not possible:
	///		- Use <see cref="UIPlayerRootForwarder"/>.
	///		- Make a prefab variant with added component and instantiate that one instead.
	///		- Have the prefab saved as inactive. Add the component after instantiation, then activate the prefab.
	///		- If not working with prefabs - add the component to the scene UI root game object, instantiate it and destroy the original.
	///
	/// Additionally, there is a global fall-back instance automatically created for any objects that do not belong to any player.
	/// For this reason, for single player game don't attach any components.
	///
	/// Note: this is how Unity UI can support multiple players each having their own UI selection and navigation.
	///		  Basically they are limited by the <see cref="UnityEngine.InputSystem.UI.MultiplayerEventSystem.playerRoot"/>
	///		  Good explanation: https://www.youtube.com/watch?v=Ur2tBl58YOc
	///		  Also: https://opsive.com/support/documentation/ultimate-inventory-system/input/split-screen-co-op-ui/
	/// </summary>
	public class UIPlayerRootObject : MonoBehaviour, IPlayerRoot
	{

		/// <summary>
		/// Global root object used by any components not owned by any player.
		/// You may want to set this as DontDestroyOnLoad() or parent it under your input system.
		/// </summary>
		public static UIPlayerRootObject GlobalUIRootObject {
			get {
				if (m_GlobalUIRootObject == null) {
					m_GlobalUIRootObject = new GameObject(nameof(GlobalUIRootObject)).AddComponent<UIPlayerRootObject>();
					m_GlobalUIRootObject.m_PlayerIndex = PlayerIndex.MasterPlayer;
				}

				return m_GlobalUIRootObject;
			}
		}
		private static UIPlayerRootObject m_GlobalUIRootObject;

		/// <summary>
		/// Root objects for all the players, excluding the Global one.
		/// </summary>
		public static IReadOnlyCollection<UIPlayerRootObject> PlayerUIRoots => m_PlayerRootObjects;
		private static List<UIPlayerRootObject> m_PlayerRootObjects = new List<UIPlayerRootObject>();

		/// <summary>
		/// All player root objects, including the Global one.
		/// </summary>
		public static IEnumerable<UIPlayerRootObject> AllPlayerUIRoots {
			get {
				yield return GlobalUIRootObject;

				foreach(UIPlayerRootObject rootObject in m_PlayerRootObjects) {
					yield return rootObject;
				}
			}
		}

		/// <summary>
		/// Is the player setup ready.
		/// </summary>
		public bool IsActive => EventSystem != null;

		/// <summary>
		/// Event system used by this player.
		/// </summary>
		public EventSystem EventSystem => m_EventSystem;
		[SerializeField] private EventSystem m_EventSystem;

		/// <summary>
		/// Player Index of the owner player.
		/// </summary>
		public PlayerIndex PlayerIndex => m_PlayerIndex;
		[SerializeField] private PlayerIndex m_PlayerIndex = PlayerIndex.Player0;

		private List<object> m_ContextReferences = new List<object>();

		/// <summary>
		/// Short-cut - get selected UI object for this player.
		/// </summary>
		public GameObject SelectedGameObject => EventSystem?.currentSelectedGameObject;

		/// <summary>
		/// Short-cut - set selected UI object for this player.
		/// </summary>
		public void SetSelectedGameObject(GameObject selected) => EventSystem?.SetSelectedGameObject(selected);

		/// <summary>
		/// Get the top-most root object.
		/// </summary>
		public UIPlayerRootObject GetRootObject() => this;

		/// <summary>
		/// Get the owning player root object. If no owner found, <see cref="GlobalUIRootObject"/> is returned.
		/// </summary>
		/// <param name="go">target object needing player owner</param>
		public static IPlayerRoot GetPlayerUIRootFor(GameObject go)
		{
			var rootObject = go.transform.GetComponentInParent<IPlayerRoot>(true);
			if (rootObject != null) {
				if (rootObject is UIPlayerRootObject uiRootObject && !m_PlayerRootObjects.Contains(uiRootObject)) {
					m_PlayerRootObjects.Add(uiRootObject);
				}

				return rootObject;
			}

			return GlobalUIRootObject;
		}

		/// <summary>
		/// Add arbitrary object to this player root. Useful for attaching level state stack or similar per player.
		/// </summary>
		public void AddContextReference(object reference) => m_ContextReferences.Add(reference);

		/// <summary>
		/// Get arbitrary object from this player root. Useful for attaching level state stack or similar per player.
		/// </summary>
		public T GetContextReference<T>() => m_ContextReferences.OfType<T>().FirstOrDefault();

		/// <summary>
		/// Remove arbitrary object from this player root. Useful for attaching level state stack or similar per player.
		/// </summary>
		public bool RemoveContextReference(object reference) => m_ContextReferences.Remove(reference);

		/// <summary>
		/// Remove arbitrary object from this player root. Useful for attaching level state stack or similar per player.
		/// </summary>
		public bool RemoveContextReference<T>() {
			int index = m_ContextReferences.FindIndex(obj => obj is T);
			if (index >= 0) {
				m_ContextReferences.RemoveAt(index);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Remove all context references.
		/// </summary>
		public void ClearContextReferences() => m_ContextReferences.Clear();


		/// <summary>
		/// Called when assembly reload is disabled.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStaticsCache()
		{
			m_GlobalUIRootObject = null;
			m_PlayerRootObjects.Clear();
		}

		protected virtual void Awake()
		{
			if (!m_PlayerRootObjects.Contains(this)) {
				m_PlayerRootObjects.Add(this);
			}
		}

		protected virtual void OnDestroy()
		{
			m_PlayerRootObjects.Remove(this);
			m_EventSystem = null;
		}

		/// <summary>
		/// Set the owning event system for the <see cref="GlobalUIRootObject"/>.
		/// Will attach the player root to the EventSystem.
		/// </summary>
		public void SetupGlobal(EventSystem eventSystem)
		{
			if (this != m_GlobalUIRootObject) {
				throw new System.InvalidOperationException($"Trying to setup {name} as global player root object. Please use \"{nameof(SetupPlayer)}()\"");
			}

			m_EventSystem = eventSystem;
			m_PlayerIndex = PlayerIndex.MasterPlayer;

			transform.SetParent(eventSystem.transform);
		}

		/// <summary>
		/// Set the owning event system & player index.
		/// </summary>
		public void SetupPlayer(EventSystem eventSystem, PlayerIndex playerIndex)
		{
			if (this == m_GlobalUIRootObject) {
				throw new System.InvalidOperationException($"Trying to setup {name} as non-global player root object. Please use \"{nameof(SetupGlobal)}()\"");
			}

			if (playerIndex < PlayerIndex.Player0) {
				Debug.LogError($"Trying to setup event system root object \"{name}\" with invalid player index: {playerIndex}", this);
			}

			m_EventSystem = eventSystem;
			m_PlayerIndex = playerIndex;
		}
	}
}