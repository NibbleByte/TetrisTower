using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using DevLocker.GFrame.Utils;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Isolates all child selectables into a separate navigation group (can't navigate out from it, unless specified to).
	/// NOTE: outside selectables can still navigate to this group.
	/// </summary>
	public class UINavigationGroup : MonoBehaviour
	{
		public enum WrapMode
		{
			None = 0,
			Wrap = 5,
			Auto = 10,
			Explicit = 15,
			AutoSelectableOfNavigationGroup = 20,
			FirstSelectableOfNavigationGroup = 24,   // NOTE: These might not be what you expect if arrangement is more irregular...
			LastSelectableOfNavigationGroup = 28,    // NOTE: These might not be what you expect if arrangement is more irregular...
			FocusScope = 40,
			TriggerEvent = 50,
		}

		public enum NavigationMode
		{
			Grid,
			Horizontal,
			Vertical,
		}

		[Serializable]
		public struct WrapBehaviour
		{
			public WrapMode Mode;
			public bool IsNoneMode => Mode == WrapMode.None;
			public bool IsWrapMode => Mode == WrapMode.Wrap;
			public bool IsAutoMode => Mode == WrapMode.Auto;
			public bool IsExplicitMode => Mode == WrapMode.Explicit;
			public bool IsFirstSelectableOfNavigationGroupMode => Mode == WrapMode.FirstSelectableOfNavigationGroup;
			public bool IsLastSelectableOfNavigationGroupMode => Mode == WrapMode.LastSelectableOfNavigationGroup;
			public bool IsFocusScopeMode => Mode == WrapMode.FocusScope;
			public bool IsTriggerEventMode => Mode == WrapMode.TriggerEvent;

			public Selectable Selectable;
			public UINavigationGroup NavigationGroup;
			public UIScope Scope;
			public UnityEvent Event;
		}

		// IMoveHandler and other events are received only if this is the selected object.
		// Also, yes, nested MonoBehaviours are allowed!
		protected internal class UINavigationListener : MonoBehaviour, IMoveHandler
		{
			public UINavigationGroup Owner;

			public void OnMove(AxisEventData eventData)
			{
				if (Owner) {
					Owner.OnMove(eventData);
				}
			}
		}

		[Header("Wrap Behaviours")]
		public WrapBehaviour WrapUp;
		public WrapBehaviour WrapDown;
		public WrapBehaviour WrapLeft;
		public WrapBehaviour WrapRight;

		[Header("Details")]
		[Tooltip("Scan for added or removed selectables on Update(). If disabled, call methods manually on change.\nNOTE: Changing positions of selectables will always require manual notification.")]
		public bool AutoScanForSelectables = true;

		public NavigationMode Navigation = NavigationMode.Grid;

		[Tooltip("Skip navigation wrapping if last move happened sooner than this threshold. Useful for stopping continues navigation before user wraps away the current group. 0 will not skip anything. 0.5 is a good value.")]
		public float SkipWrapTimeTreshold = 0f;


		[Tooltip("Include these selectables to navigation links, in addition to any group children.\nUseful to include special selectables from outside the group, but not all outsiders.")]
		public List<Selectable> Include;

		[Tooltip("Exclude these selectables from navigation links.")]
		public List<Selectable> Exclude;

		private const int s_StartActiveSelectablesSize = 10; // Lengths must be equal!
		private int m_AllActiveSelectablesCount = 0;
		private Selectable[] m_AllActiveSelectables;	// Currently used.
		private Selectable[] m_AllActiveSelectables1 = new Selectable[s_StartActiveSelectablesSize];
		private Selectable[] m_AllActiveSelectables2 = new Selectable[s_StartActiveSelectablesSize];
		private bool[] m_AllActiveSelectablesInteractablility = new bool[s_StartActiveSelectablesSize];

		private List<Selectable> m_ManagedSelectables = null;

		public IReadOnlyList<Selectable> ManagedSelectables {
			get {
				if (m_ManagedSelectables == null) {
					RefreshNavigationSelectables();
				}

				// If waiting for canvas layout, try to get what we have up till now. Or rebuild just finished this frame but we didn't know yet.
				if (m_CanvasLayoutIsRebuilding) {
					RefreshNavigationSelectables();

					if (UIUtils.IsLayoutRebuildPending()) {
						RefreshNavigationLinkPositions();
					}
				}

				return m_ManagedSelectables;
			}
		}

		/// <summary>
		/// Most top-left selectable.
		/// </summary>
		public Selectable FirstSelectable {
			get {
				if (m_FirstSelectable == null || !m_FirstSelectable.isActiveAndEnabled) {
					RefreshNavigationSelectables();
				}

				// If waiting for canvas layout, try to get what we have up till now. Or rebuild just finished this frame but we didn't know yet.
				if (m_CanvasLayoutIsRebuilding) {
					RefreshNavigationSelectables();

					if (UIUtils.IsLayoutRebuildPending()) {
						RefreshNavigationLinkPositions();
					}
				}

				return m_FirstSelectable;
			}
		}

		/// <summary>
		/// Most bottom-right selectable.
		/// </summary>
		public Selectable LastSelectable {
			get {
				if (m_LastSelectable == null || !m_LastSelectable.isActiveAndEnabled) {
					RefreshNavigationSelectables();
				}

				// If waiting for canvas layout, try to get what we have up till now. Or rebuild just finished this frame but we didn't know yet.
				if (m_CanvasLayoutIsRebuilding) {
					RefreshNavigationSelectables();

					if (UIUtils.IsLayoutRebuildPending()) {
						RefreshNavigationLinkPositions();
					}
				}

				return m_LastSelectable;
			}
		}

		private Selectable m_FirstSelectable = null;
		private Selectable m_LastSelectable = null;

		private Dictionary<int, List<MoveDirection>> m_OwnedEdgeSelectableIds = new Dictionary<int, List<MoveDirection>>();

		private float m_LastMove = -1f;

		private GameObject m_LastSelectedObject;
		private EventSystem m_CurrentEventSystem;

		private bool m_CanvasLayoutIsRebuilding = false;

		protected virtual void Reset()
		{
			var selectionController = GetComponent<SelectionController>();
			if (selectionController && (selectionController.StartSelections?.Count ?? 0) == 0 && (selectionController.StartNavigationGroups?.Count ?? 0) == 0) {
				selectionController.StartSelectionSource = SelectionController.StartSelectionSourceTypes.NavigationGroups;
				selectionController.StartNavigationGroups = new List<UINavigationGroup>();
				selectionController.StartNavigationGroups.Add(this);

#if UNITY_EDITOR
				EditorUtility.SetDirty(selectionController);
#endif
			}
		}

		protected virtual void OnDestroy()
		{
			// Remove references for easier memory profiling and debugging. NOTE: if object was never awaken, this won't get executed.
			m_FirstSelectable = null;
			m_LastSelectable = null;

			WrapUp = default;
			WrapDown = default;
			WrapLeft = default;
			WrapRight = default;

			m_AllActiveSelectables = null;
			m_AllActiveSelectables1 = null;
			m_AllActiveSelectables2 = null;
			m_ManagedSelectables?.Clear();

			m_LastSelectedObject = null;

			Include.Clear();
			Exclude.Clear();
		}

		/// <summary>
		/// Call this in dire situations (re-parenting selectables).
		/// Will clear caches and rescan for selectables on the next frame if auto-scan enabled, or immediately if not.
		/// </summary>
		public void RefreshNavigationClearCaches()
		{
			m_ManagedSelectables?.Clear();
			m_ManagedSelectables = null;

			m_AllActiveSelectablesCount = 0;
			m_AllActiveSelectables = null;    // Currently used.
			m_AllActiveSelectables1 = new Selectable[s_StartActiveSelectablesSize];
			m_AllActiveSelectables2 = new Selectable[s_StartActiveSelectablesSize];
			m_AllActiveSelectablesInteractablility = new bool[s_StartActiveSelectablesSize];

			m_FirstSelectable = null;
			m_LastSelectable = null;

			if (!AutoScanForSelectables) {
				RefreshNavigationSelectables();
			}
		}

		/// <summary>
		/// Call this if you have added or removed child selectables and have <see cref="AutoScanForSelectables"/> disabled (no updates, no polling).
		/// NOTE: This won't work if positions changed or selectables changed parents.
		/// </summary>
		public bool RefreshNavigationSelectables()
		{
			bool needsRefresh = false;
			bool hasAutoWrap = WrapUp.IsAutoMode || WrapDown.IsAutoMode || WrapLeft.IsAutoMode || WrapRight.IsAutoMode;

			if (m_ManagedSelectables == null) {
				m_ManagedSelectables = new List<Selectable>();
			}

			if (Application.isPlaying) {

				// Gather active selectables from UGUI API. Note that this list incudes interactable and non-interactable ones.
				// If auto wrap is used, check if selectables objects changed or their interactability.

				if (m_AllActiveSelectables == null) {
					m_AllActiveSelectables = m_AllActiveSelectables2;
				}

				if (m_AllActiveSelectables.Length < Selectable.allSelectableCount) {
					// Must all have equal size.
					m_AllActiveSelectables1 = new Selectable[Selectable.allSelectableCount * 2];
					m_AllActiveSelectables2 = new Selectable[Selectable.allSelectableCount * 2];
					m_AllActiveSelectablesInteractablility = new bool[Selectable.allSelectableCount * 2];

					m_AllActiveSelectables = m_AllActiveSelectables2;
				}

				Selectable[] prevActiveSelectables = m_AllActiveSelectables;
				int prevActiveSelectablesCount = m_AllActiveSelectablesCount;
				m_AllActiveSelectables = m_AllActiveSelectables == m_AllActiveSelectables1 ? m_AllActiveSelectables2 : m_AllActiveSelectables1;

				m_AllActiveSelectablesCount = Selectable.AllSelectablesNoAlloc(m_AllActiveSelectables);

				if (hasAutoWrap) {
					// Auto-wraps will link selectables outside this group that are not managed.
					// If linked selectable becomes unavailable or a new one becomes available, auto should be refreshed.
					// So check if list of selectables changed. Later will be checked for interactability too.

					if (prevActiveSelectablesCount == m_AllActiveSelectablesCount) {
						// Both arrays should have the same length! Always!
						for (int i = 0; i < m_AllActiveSelectablesCount; ++i) {
							if (prevActiveSelectables[i] != m_AllActiveSelectables[i]) {
								needsRefresh = true;
								// Don't break or continue! Need to write interactable flags.
							}

							// Check if button was active but not interactable and became interactable.
							bool interactable = m_AllActiveSelectables[i].IsInteractable();
							if (m_AllActiveSelectablesInteractablility[i] != interactable) {
								needsRefresh = true;
							}

							m_AllActiveSelectablesInteractablility[i] = interactable;
						}
					} else {
						needsRefresh = true;
					}
				}

			} else {
				m_AllActiveSelectables = GetComponentsInChildren<Selectable>().Concat(Include).Distinct().ToArray();
				m_AllActiveSelectablesCount = m_AllActiveSelectables.Length;
				m_AllActiveSelectablesInteractablility = m_AllActiveSelectables.Select(s => s.IsInteractable()).ToArray();
			}

			// Remove destroyed or inactive selectables.
			for (int i = 0; i < m_ManagedSelectables.Count; ++i) {
				Selectable selectable = m_ManagedSelectables[i];

				// Destroyed - yes. Re-parented - no. :(
				// Check active instead gameObject.activeInHierarchy hoping it will be faster.
				if (selectable == null || !selectable.IsInteractable() || !IsStillActive(selectable)) {

					if (selectable) {
						var listener = selectable.gameObject.GetComponent<UINavigationListener>();
						listener.Owner = null;
					}

					m_ManagedSelectables.RemoveAt(i);
					--i;
					needsRefresh = true;
					continue;
				}

				// Check for interactability. If object has become inactive it would be detected above.
				if (!needsRefresh && selectable && IsAutoWrappedNotInteractable(selectable)) {
					needsRefresh = true;
				}
			}

			// Check for new selectables.
			for(int i = 0; i < m_AllActiveSelectablesCount; ++i) {
				Selectable selectable = m_AllActiveSelectables[i];

				// End of array... or destroyed?
				if (selectable == null)
					break;

				if (!Exclude.Contains(selectable)
				    && selectable.IsInteractable()
					&& !m_ManagedSelectables.Contains(selectable)
				    && selectable.GetComponent<UINavigationGroupExclude>() == null
					&& (Include.Contains(selectable) || selectable.GetComponentInParent<UINavigationGroup>() == this)
					) {
					m_ManagedSelectables.Add(selectable);

					// Make sure to set navigation mode different from None in advance or the Selectable.FindSelectable() below will skip it.
					Navigation nav = selectable.navigation;
					nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
					selectable.navigation = nav;

					if (Application.isPlaying) {
						// Never removed but who cares...
						var listener = selectable.gameObject.GetComponent<UINavigationListener>();
						if (listener == null) {
							listener = selectable.gameObject.AddComponent<UINavigationListener>();
						}

						listener.Owner = this;
					}

					needsRefresh = true;
				}
			}

			// If canvas layout rebuild is pending, wait for it to finish, then do the calculations.
			if (m_CanvasLayoutIsRebuilding && UIUtils.IsLayoutRebuildPending()) {
				return true;

			} else if (m_CanvasLayoutIsRebuilding) {
				m_CanvasLayoutIsRebuilding = false;
				needsRefresh = true;
			}

			if (needsRefresh) {
				RefreshNavigationLinkPositions();
			}

			return needsRefresh;
		}

		/// <summary>
		/// Will re-evaluate all managed navigation links.
		/// Call this if you have moved the selectables around (this won't be detected automatically)
		/// </summary>
		public void RefreshNavigationLinkPositions()
		{
			if (m_ManagedSelectables == null) {
				// Will eventually call this method, but will setup cache properly.
				RefreshNavigationSelectables();
				return;
			}

			m_FirstSelectable = m_LastSelectable = m_ManagedSelectables.FirstOrDefault();

#if UNITY_EDITOR
			if (!Application.isPlaying) {
				Undo.RecordObjects(m_ManagedSelectables.ToArray(), this.ToString());
			}
#endif

			m_OwnedEdgeSelectableIds.Clear();

			for (int i = 0; i < m_ManagedSelectables.Count; ++i) {
				Selectable selectable = m_ManagedSelectables[i];

				// Destroyed - yes. Re-parented - no. :(
				if (selectable == null) {
					m_ManagedSelectables.RemoveAt(i);
					--i;
					continue;
				}

				Navigation nav = selectable.navigation;
				nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

				//
				// https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Selectable.html#UnityEngine_UI_Selectable_FindSelectable_UnityEngine_Vector3_
				// Don't use FindSelectableOn*() as it won't work if "Explicit" mode set.
				nav.selectOnUp = (Navigation == NavigationMode.Grid || Navigation == NavigationMode.Vertical) ? FindManagedSelectable(selectable, Vector3.up) : null;
				nav.selectOnDown = (Navigation == NavigationMode.Grid || Navigation == NavigationMode.Vertical) ? FindManagedSelectable(selectable, Vector3.down) : null;
				nav.selectOnLeft = (Navigation == NavigationMode.Grid || Navigation == NavigationMode.Horizontal) ? FindManagedSelectable(selectable, Vector3.left) : null;
				nav.selectOnRight = (Navigation == NavigationMode.Grid || Navigation == NavigationMode.Horizontal) ? FindManagedSelectable(selectable, Vector3.right) : null;

				bool outsideUp = nav.selectOnUp == null;
				bool outsideDown = nav.selectOnDown == null;
				bool outsideLeft = nav.selectOnLeft == null;
				bool outsideRight = nav.selectOnRight == null;

				// NOTE: These might not be what you expect if arrangement is more irregular...
				if (outsideUp && outsideLeft) {
					m_FirstSelectable = selectable;
				}

				// NOTE: These might not be what you expect if arrangement is more irregular...
				if (outsideDown && outsideRight) {
					m_LastSelectable = selectable;
				}

				if (outsideUp) RecordEdgeSelectable(selectable.gameObject, MoveDirection.Up);
				if (outsideDown) RecordEdgeSelectable(selectable.gameObject, MoveDirection.Down);
				if (outsideLeft) RecordEdgeSelectable(selectable.gameObject, MoveDirection.Left);
				if (outsideRight) RecordEdgeSelectable(selectable.gameObject, MoveDirection.Right);

				// Outside means no managed selectable is found. Find any selectable then.
				if (outsideUp && WrapUp.IsAutoMode) nav.selectOnUp = selectable.FindSelectable(selectable.transform.rotation * Vector3.up);
				if (outsideDown && WrapDown.IsAutoMode) nav.selectOnDown = selectable.FindSelectable(selectable.transform.rotation * Vector3.down);
				if (outsideLeft && WrapLeft.IsAutoMode) nav.selectOnLeft = selectable.FindSelectable(selectable.transform.rotation * Vector3.left);
				if (outsideRight && WrapRight.IsAutoMode) nav.selectOnRight = selectable.FindSelectable(selectable.transform.rotation * Vector3.right);

				if (nav.selectOnUp == null && WrapUp.IsExplicitMode) nav.selectOnUp = WrapUp.Selectable;
				if (nav.selectOnDown == null && WrapDown.IsExplicitMode) nav.selectOnDown = WrapDown.Selectable;
				if (nav.selectOnLeft == null && WrapLeft.IsExplicitMode) nav.selectOnLeft = WrapLeft.Selectable;
				if (nav.selectOnRight == null && WrapRight.IsExplicitMode) nav.selectOnRight = WrapRight.Selectable;

				selectable.navigation = nav;
			}

			//
			// Now do it again to setup wrapping wrap modes, after all links are setup.
			//
			if (WrapUp.IsWrapMode || WrapDown.IsWrapMode || WrapLeft.IsWrapMode || WrapRight.IsWrapMode) {

				for (int i = 0; i < m_ManagedSelectables.Count; ++i) {
					Selectable selectable = m_ManagedSelectables[i];

					Navigation nav = selectable.navigation;
					nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;

					int sanityCount = 0;
					const int sanityCountLimit = 10000;

					if (nav.selectOnUp == null && WrapUp.IsWrapMode) {
						Selectable it = selectable;
						while(it.navigation.selectOnDown != null && it.navigation.selectOnDown.navigation.selectOnUp == it && it.navigation.selectOnDown != selectable) {
							it = it.navigation.selectOnDown;

							sanityCount++;
							if (sanityCount > sanityCountLimit) {
								Debug.LogError($"[Input] Navigation group couldn't wrap around {selectable}!", this);
								break;
							}
						}

						if (selectable != it) {
							nav.selectOnUp = it;
						}
					}


					if (nav.selectOnDown == null && WrapDown.IsWrapMode) {
						Selectable it = selectable;
						while(it.navigation.selectOnUp != null && it.navigation.selectOnUp.navigation.selectOnDown == it && it.navigation.selectOnUp != selectable) {
							it = it.navigation.selectOnUp;

							sanityCount++;
							if (sanityCount > sanityCountLimit) {
								Debug.LogError($"[Input] Navigation group couldn't wrap around {selectable}!", this);
								break;
							}
						}

						if (selectable != it) {
							nav.selectOnDown = it;
						}
					}


					if (nav.selectOnLeft == null && WrapLeft.IsWrapMode) {
						Selectable it = selectable;
						while(it.navigation.selectOnRight != null && it.navigation.selectOnRight.navigation.selectOnLeft == it && it.navigation.selectOnRight != selectable) {
							it = it.navigation.selectOnRight;

							sanityCount++;
							if (sanityCount > sanityCountLimit) {
								Debug.LogError($"[Input] Navigation group couldn't wrap around {selectable}!", this);
								break;
							}
						}

						if (selectable != it) {
							nav.selectOnLeft = it;
						}
					}


					if (nav.selectOnRight == null && WrapRight.IsWrapMode) {
						Selectable it = selectable;
						while(it.navigation.selectOnLeft != null && it.navigation.selectOnLeft.navigation.selectOnRight == it && it.navigation.selectOnLeft != selectable) {
							it = it.navigation.selectOnLeft;

							sanityCount++;
							if (sanityCount > sanityCountLimit) {
								Debug.LogError($"[Input] Navigation group couldn't wrap around {selectable}!", this);
								break;
							}
						}

						if (selectable != it) {
							nav.selectOnRight = it;
						}
					}

					selectable.navigation = nav;
				}
			}
		}

		private bool IsStillActive(Selectable selectable)
		{
			for(int i = 0; i < m_AllActiveSelectablesCount; ++i) {
				Selectable activeSelectable = m_AllActiveSelectables[i];

				if (activeSelectable == null) return false;	// Reached the end of the array.
				if (activeSelectable == selectable) return true;
			}

			return false;
		}

		private bool IsAutoWrappedNotInteractable(Selectable selectable)
		{
			Navigation nav = selectable.navigation;
			if (WrapUp.IsAutoMode && nav.selectOnUp && !nav.selectOnUp.IsInteractable()) return true;
			if (WrapDown.IsAutoMode && nav.selectOnDown && !nav.selectOnDown.IsInteractable()) return true;
			if (WrapLeft.IsAutoMode && nav.selectOnLeft && !nav.selectOnLeft.IsInteractable()) return true;
			if (WrapRight.IsAutoMode && nav.selectOnRight && !nav.selectOnRight.IsInteractable()) return true;

			return false;
		}

#if UNITY_EDITOR
		[ContextMenu("Apply Group Navigation")]
		internal void ApplyGroupNavigation()
		{
			RefreshNavigationClearCaches();
			RefreshNavigationSelectables();
		}
#endif

		private Selectable FindManagedSelectable(Selectable selectable, Vector3 dir)
		{
			// Selectable.FindSelectable() searches only interactable ones.
			while ((selectable = selectable.FindSelectable(selectable.transform.rotation * dir)) != null) {
				if (m_ManagedSelectables.Contains(selectable))
					return selectable;
			}

			return null;
		}

		private void RecordEdgeSelectable(GameObject go, MoveDirection direction)
		{
			int id = go.GetInstanceID();

			if (!m_OwnedEdgeSelectableIds.TryGetValue(id, out List<MoveDirection> directions)) {
				directions = new List<MoveDirection>();
				m_OwnedEdgeSelectableIds.Add(id, directions);
			}

			directions.Add(direction);
		}

		protected virtual GameObject GetCurrentlySelectedObject()
		{
			return m_CurrentEventSystem
				? m_CurrentEventSystem.currentSelectedGameObject
				: EventSystem.current?.currentSelectedGameObject
				;
		}

		void Update()
		{
			m_LastSelectedObject = GetCurrentlySelectedObject();

			if (AutoScanForSelectables) {
				if (UIUtils.IsLayoutRebuildPending()) {
					m_CanvasLayoutIsRebuilding = true;
				}

				RefreshNavigationSelectables();
			}
		}

		public void OnMove(AxisEventData eventData)
		{
			// In case of multiple event system, movement will happen from the "current" that owns me.
			// Used in split-screen setup.
			m_CurrentEventSystem = EventSystem.current;

			if (SkipWrapTimeTreshold > 0f) {
				m_OwnedEdgeSelectableIds.TryGetValue(m_LastSelectedObject.GetInstanceID(), out List<MoveDirection> edgeDirections);

				if (edgeDirections != null && edgeDirections.Contains(eventData.moveDir) && Time.time - m_LastMove <= SkipWrapTimeTreshold) {
					eventData.selectedObject = m_LastSelectedObject;
					m_LastMove = Time.time; // Still counts as move - prevent the following calls of continues navigation as well.
					return;
				}
			}

			m_LastMove = Time.time;

			// OnMove() event is called AFTER selected was changed, so you want to skip the initial call.
			// (they are rather called together, but Selectable.Navigate() is first and it doesn't use / consume the event).
			if (m_LastSelectedObject != GetCurrentlySelectedObject())
				return;

			Selectable selectable = GetCurrentlySelectedObject()?.GetComponent<Selectable>();

			if (eventData.used || m_ManagedSelectables == null || !m_ManagedSelectables.Contains(selectable))
				return;

			switch (eventData.moveDir) {
				case MoveDirection.Up:
					OnMoveWrapDynamic(WrapUp, selectable.navigation.selectOnUp, eventData);
					break;

				case MoveDirection.Down:
					OnMoveWrapDynamic(WrapDown, selectable.navigation.selectOnDown, eventData);
					break;

				case MoveDirection.Left:
					OnMoveWrapDynamic(WrapLeft, selectable.navigation.selectOnLeft, eventData);
					break;

				case MoveDirection.Right:
					OnMoveWrapDynamic(WrapRight, selectable.navigation.selectOnRight, eventData);
					break;
			}
		}

		private void OnMoveWrapDynamic(WrapBehaviour wrapBehaviour, bool hasWrapLink, AxisEventData eventData)
		{
			if (hasWrapLink)
				return;

			Selectable nextSelectable;

			switch (wrapBehaviour.Mode) {
				case WrapMode.None:
				case WrapMode.Wrap:
				case WrapMode.Auto:
				case WrapMode.Explicit:
					// Do nothing, they should be linked already.
					return;

				case WrapMode.AutoSelectableOfNavigationGroup:
					var eventSelectable = eventData.selectedObject.GetComponent<Selectable>();
					nextSelectable = null;
					if (wrapBehaviour.NavigationGroup && wrapBehaviour.NavigationGroup.isActiveAndEnabled) {
						nextSelectable = wrapBehaviour.NavigationGroup.FindManagedSelectable(eventSelectable, eventData.moveVector);

						// If navgroup is on the opposite side, search that way, then find the furthest edge selectable.
						if (nextSelectable == null) {
							nextSelectable = wrapBehaviour.NavigationGroup.FindManagedSelectable(eventSelectable, -eventData.moveVector);

							// Failed to find any appropriate selectable from that group.
							if (nextSelectable == null) {
								eventData.selectedObject = eventSelectable.gameObject;
								eventData.Use();
								return;
							}

							int sanityCount = 0;
							const int sanityCountLimit = 10000;

							Selectable it = nextSelectable;
							while (it) {

								Selectable itNext = eventData.moveDir switch {
									// Move in the opposite direction!!!
									MoveDirection.Up => it.navigation.selectOnDown,
									MoveDirection.Down => it.navigation.selectOnUp,
									MoveDirection.Left => it.navigation.selectOnRight,
									MoveDirection.Right => it.navigation.selectOnLeft,
									_ => throw new NotSupportedException(eventData.moveDir.ToString())
								};

								// Edge or looped links.
								if (itNext == null || itNext == nextSelectable)
									break;

								it = itNext;

								sanityCount++;
								if (sanityCount > sanityCountLimit) {
									Debug.LogError($"[Input] Navigation group couldn't wrap around {eventSelectable} for group {wrapBehaviour.NavigationGroup}!", this);
									eventData.selectedObject = eventSelectable.gameObject;
									eventData.Use();
									return;
								}
							}

							nextSelectable = it;
						}
					}

					if (nextSelectable && nextSelectable.gameObject.activeInHierarchy) {
						eventData.selectedObject = nextSelectable.gameObject;
						eventData.Use();
					}
					break;

				case WrapMode.FirstSelectableOfNavigationGroup:
					nextSelectable = wrapBehaviour.NavigationGroup ? wrapBehaviour.NavigationGroup.FirstSelectable : null;

					if (nextSelectable && nextSelectable.gameObject.activeInHierarchy) {
						eventData.selectedObject = nextSelectable.gameObject;
						eventData.Use();
					}
					break;

				case WrapMode.LastSelectableOfNavigationGroup:
					nextSelectable = wrapBehaviour.NavigationGroup ? wrapBehaviour.NavigationGroup.LastSelectable : null;

					if (nextSelectable && nextSelectable.gameObject.activeInHierarchy) {
						eventData.selectedObject = nextSelectable.gameObject;
						eventData.Use();
					}
					break;

				case WrapMode.FocusScope:
					// It already checks for activeInHierarchy.
					if (wrapBehaviour.Scope) {
						wrapBehaviour.Scope.Focus();
						eventData.Use();
					}
					break;

				case WrapMode.TriggerEvent:
					wrapBehaviour.Event?.Invoke();
					eventData.Use();
					break;

				default: throw new NotSupportedException(wrapBehaviour.Mode.ToString());
			}
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(UINavigationGroup.WrapBehaviour))]
	internal class NavigationGroupWrapBehaviourPropertyDrawer : PropertyDrawer
	{
		private static UINavigationGroup.WrapMode[] s_WrapModeValues = (UINavigationGroup.WrapMode[]) Enum.GetValues(typeof(UINavigationGroup.WrapMode));

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var mode = s_WrapModeValues[property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)).enumValueIndex];
			switch (mode) {
				case UINavigationGroup.WrapMode.None:
				case UINavigationGroup.WrapMode.Wrap:
				case UINavigationGroup.WrapMode.Auto:
				case UINavigationGroup.WrapMode.Explicit:
				case UINavigationGroup.WrapMode.AutoSelectableOfNavigationGroup:
				case UINavigationGroup.WrapMode.FirstSelectableOfNavigationGroup:
				case UINavigationGroup.WrapMode.LastSelectableOfNavigationGroup:
				case UINavigationGroup.WrapMode.FocusScope:
					return EditorGUIUtility.singleLineHeight;
				case UINavigationGroup.WrapMode.TriggerEvent:
					return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Event)));
				default:
					throw new NotSupportedException(mode.ToString());
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			var mode = s_WrapModeValues[property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)).enumValueIndex];

			switch (mode) {
				case UINavigationGroup.WrapMode.None:
				case UINavigationGroup.WrapMode.Wrap:
				case UINavigationGroup.WrapMode.Auto:
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)), new GUIContent());
					break;

				case UINavigationGroup.WrapMode.Explicit:
					position.width /= 2f;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)), new GUIContent());
					position.x += position.width;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Selectable)), new GUIContent());
					break;

				case UINavigationGroup.WrapMode.AutoSelectableOfNavigationGroup:
				case UINavigationGroup.WrapMode.FirstSelectableOfNavigationGroup:
				case UINavigationGroup.WrapMode.LastSelectableOfNavigationGroup:
					position.width /= 2f;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)), new GUIContent());
					position.x += position.width;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.NavigationGroup)), new GUIContent());
					break;

				case UINavigationGroup.WrapMode.FocusScope:
					position.width /= 2f;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)), new GUIContent());
					position.x += position.width;
					EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Scope)), new GUIContent());
					break;

				case UINavigationGroup.WrapMode.TriggerEvent:
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Mode)), new GUIContent());
					position.height -= EditorGUIUtility.singleLineHeight;
					position.y += EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative(nameof(UINavigationGroup.WrapBehaviour.Event)), new GUIContent());
					break;

				default:
					throw new NotSupportedException(mode.ToString());
			}

			EditorGUI.EndProperty();
		}
	}

	[CustomEditor(typeof(UINavigationGroup))]
	[CanEditMultipleObjects]
	internal class NavigationGroupEditor : Editor
	{
		/// <summary>
		/// Copy-pasted logic for drawing navigation links from <see cref="SelectableEditor"/> - s_ShowNavigation.
		/// </summary>


		private static List<NavigationGroupEditor> s_Editors = new List<NavigationGroupEditor>();
		private static bool s_ShowNavigation = false;
		private static string s_ShowNavigationKey = "SelectableEditor.ShowNavigation";
		private static System.Reflection.MethodInfo s_StaticOnSceneGUIMethod;
		private GUIContent m_VisualizeNavigation = EditorGUIUtility.TrTextContent("Visualize", "Show navigation flows between selectable UI elements.");

		protected virtual void OnEnable()
		{
			s_Editors.Add(this);
			//RegisterStaticOnSceneGUI();

			if (s_Editors.Count == 1) {
				SceneView.duringSceneGui += StaticOnSceneGUI;
			}

			s_ShowNavigation = EditorPrefs.GetBool(s_ShowNavigationKey);
			SyncShowNavigation();
		}

		protected virtual void OnDisable()
		{
			s_Editors.Remove(this);
			//RegisterStaticOnSceneGUI();

			if (s_Editors.Count == 0) {
				SceneView.duringSceneGui -= StaticOnSceneGUI;
			}
		}

		// HACK: Having two inspectors causes twice the OnEnabled on select and OnDisable on deselect.
		//		 Unsubscribing multiple times in OnDisable() at the same time fails for some reason and handler keeps getting called.
		//		 Don't use this approach as it can also double-subscribe and render twice the nav-links.
		//private void RegisterStaticOnSceneGUI()
		//{
		//	SceneView.duringSceneGui -= StaticOnSceneGUI;
		//	if (s_Editors.Count > 0)
		//		SceneView.duringSceneGui += StaticOnSceneGUI;
		//}

		private void StaticOnSceneGUI(SceneView view)
		{
			if (!s_ShowNavigation)
				return;

			// HACK: Having two inspectors causes twice the OnEnabled on select and OnDisable on deselect.
			//		 Unsubscribing multiple times in OnDisable() at the same time fails for some reason and handler keeps getting called.
			if (s_Editors.Count == 0) {
				SceneView.duringSceneGui -= StaticOnSceneGUI;
			}

			if (s_StaticOnSceneGUIMethod == null) {
				Type selectableEditorType = typeof(UnityEditor.UI.SelectableEditor);
				s_StaticOnSceneGUIMethod = selectableEditorType.GetMethod("StaticOnSceneGUI", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			}

			// Something changed? Fail silently...
			if (s_StaticOnSceneGUIMethod == null)
				return;

			s_StaticOnSceneGUIMethod.Invoke(null, new object[] { view });
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUI.BeginChangeCheck();

			s_ShowNavigation = GUILayout.Toggle(s_ShowNavigation, m_VisualizeNavigation, EditorStyles.miniButton);

			if (GUILayout.Button("Apply Group Navigation")) {
				foreach(UnityEngine.Object target in targets) {
					if (target is UINavigationGroup group) {
						group.ApplyGroupNavigation();
					}
				}
			}

			if (EditorGUI.EndChangeCheck()) {
				EditorPrefs.SetBool(s_ShowNavigationKey, s_ShowNavigation);

				SyncShowNavigation();

				SceneView.RepaintAll();
			}
		}

		private static void SyncShowNavigation()
		{
			Type selectableEditorType = typeof(UnityEditor.UI.SelectableEditor);
			var s_ShowNavigationField = selectableEditorType.GetField("s_ShowNavigation", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

			// Something changed? Fail silently...
			if (s_ShowNavigationField == null)
				return;

			s_ShowNavigationField.SetValue(null, s_ShowNavigation);
		}
	}
#endif

}