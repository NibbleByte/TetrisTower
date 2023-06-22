using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Simple class for making tabbed UI + hotkeys.
	///
	/// <see cref="Tabs"/> are simple buttons. When pressed, the corresponding game object from <see cref="Contents"/> is displayed.
	/// Supports dynamically activating/deactivating tab game object.
	/// The disabled state of the button is used as "open" state of the tab.
	/// Can have hotkeys for switching between previous/next tab.
	///
	/// <see cref="Tabs"/> buttons can have additional onClick events (like sounds and particles). Hotkeys will invoke them as well (not only pointer clicks).
	/// Or just use the <see cref="TabOpened"/> and <see cref="TabClosed"/>.
	///
	/// You can fill in the lists dynamically by code then call <see cref="Reinitialize(Button)"/>.
	/// </summary>
	public class TabsGroup : MonoBehaviour
	{
		[Tooltip("Should next/previous hotkeys wrap around the available toggles.")]
		public bool WrapAround = true;

		public HotkeyEventScopeElement NextHotkey;
		public HotkeyEventScopeElement PreviousHotkey;

		[Tooltip("List here all tab buttons. They should correspond to the Contents objects.\n" +
			"Hidden inactive tab gameobjects are allowed. You can re-activate them at any time.\n" +
			"Opened tab is displayed as \"disabled\" (non-interactive) so it has distinguish look.\n" +
			"If you need to have actually disabled tabs, you'll need another implementation.")]
		public List<Button> Tabs;

		[Tooltip("Contents to be displayed when corresponding tab is active.")]
		public List<GameObject> Contents;

		[Tooltip("Which tab should be initially active?")]
		public Button StartTab;

		public bool ActivateInitialTabOnEnable = true;

		[Serializable]
		public class TabActionEvent : UnityEvent<Button> { }

		[Space()]
		public TabActionEvent TabOpened;
		public TabActionEvent TabClosed;

		public Button ActiveTab => m_CurrentIndex >= 0 ? Tabs[m_CurrentIndex] : null;
		public GameObject ActiveContent => m_CurrentIndex >= 0 ? Contents[m_CurrentIndex] : null;

		public GameObject GetContentByTab(Button tabButton) => Contents[Tabs.IndexOf(tabButton)];

		private int m_CurrentIndex = -1;

		private List<Button> m_SubscribedTabButtons = new List<Button>();

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		protected virtual void Awake()
		{
			Button disabledTab = Tabs.FirstOrDefault(b => !b.interactable && b.gameObject.activeInHierarchy);

			if (StartTab == null || !StartTab.gameObject.activeInHierarchy) {
				StartTab = disabledTab ?? Tabs.FirstOrDefault(b => b.gameObject.activeInHierarchy);
			}

			if (StartTab) {
				Reinitialize(StartTab);
			}

			NextHotkey?.OnPerformed.AddListener(OnNextAction);
			PreviousHotkey?.OnPerformed.AddListener(OnPreviousAction);

			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		/// <summary>
		/// Call this when you add or replace the tab buttons & contents dynamically.
		/// </summary>
		/// <param name="nextStartTab">The tab that should be opened.</param>
		public virtual void Reinitialize(Button nextStartTab)
		{
			OnValidate();

			// Some buttons may be destroyed by the user, new ones added later.
			m_SubscribedTabButtons.RemoveAll(b => b == null);

			for (int i = 0; i < Tabs.Count; ++i) {
				Button tabButton = Tabs[i];

				if (!m_SubscribedTabButtons.Contains(tabButton)) {
					tabButton.onClick.AddListener(() => SwitchToTab(tabButton));
				}
				tabButton.interactable = true;
				Contents[i].SetActive(false);
			}

			m_CurrentIndex = -1;	// To skip TabClosed event.
			SwitchToTab(nextStartTab);
			StartTab = nextStartTab;
		}

		// Avoid duplication with Awake().
		private bool m_EnabledOnce = false;

		protected virtual void OnEnable()
		{
			if (ActivateInitialTabOnEnable && m_EnabledOnce) {
				m_CurrentIndex = -1;	// To skip TabClosed event.

				if (StartTab && StartTab.gameObject.activeInHierarchy) {
					Reinitialize(StartTab);
				} else {
					Button tabButton = Tabs.FirstOrDefault(b => b.gameObject.activeInHierarchy);
					if (tabButton) {
						Reinitialize(tabButton);
					}
				}
			}

			m_EnabledOnce = true;
		}

		protected virtual void OnValidate()
		{
			if (Tabs == null || Contents == null)
				return;

			if (Tabs.Count != Contents.Count) {
				Debug.LogError($"\"{name}\" Tabs buttons {Tabs.Count} count is different from the Contents objects {Contents.Count}. Every tab button needs corresponding content object.", this);
				return;
			}

			if (StartTab && !Tabs.Contains(StartTab)) {
				Debug.LogError($"\"{name}\" StartTab is not part of the original Tabs.", this);
				return;
			}
		}

		/// <summary>
		/// Switch to tab directly, bypassing any onClick events.
		/// </summary>
		public void SwitchToTab(Button tabButton)
		{
			SwitchToTab(Tabs.IndexOf(tabButton));
		}

		/// <summary>
		/// Switch to tab directly, bypassing any onClick events.
		/// </summary>
		public virtual void SwitchToTab(int index)
		{
			if (m_CurrentIndex >= 0) {
				ActiveTab.interactable = true;
				ActiveContent.SetActive(false);

				TabClosed.Invoke(ActiveTab);
			}

			m_CurrentIndex = index;

			ActiveTab.interactable = false;
			ActiveContent.SetActive(true);

			TabOpened.Invoke(ActiveTab);

			if (!ActiveTab.gameObject.activeInHierarchy) {
				Debug.LogWarning($"{nameof(TabsGroup)} \"{name}\" is trying to switch to a tab {m_CurrentIndex} \"{ActiveTab.name}\" that is not active!", this);
			}
		}

		protected virtual void OnNextAction()
		{
			if (!m_PlayerContext.IsActive)
				return;

			List<Button> activeTabs = Tabs.Where(b => b.gameObject.activeInHierarchy).ToList();
			if (activeTabs.Count == 0) {
				return;
			}

			int lastIndex = activeTabs.IndexOf(ActiveTab);

			int nextIndex = WrapAround
				? (lastIndex + 1) % activeTabs.Count
				: Mathf.Clamp(lastIndex + 1, 0, activeTabs.Count - 1)
				;

			if (lastIndex != nextIndex) {
				ExecuteEvents.Execute(activeTabs[nextIndex].gameObject, new PointerEventData(m_PlayerContext.EventSystem), ExecuteEvents.pointerClickHandler);
				// Button.onClick.Invoke(); // This will ignore disabled state.
			}
		}

		protected virtual void OnPreviousAction()
		{
			if (!m_PlayerContext.IsActive)
				return;

			List<Button> activeTabs = Tabs.Where(b => b.gameObject.activeInHierarchy).ToList();
			if (activeTabs.Count == 0) {
				return;
			}

			int lastIndex = activeTabs.IndexOf(ActiveTab);

			int nextIndex = WrapAround
				? lastIndex - 1
				: Mathf.Clamp(lastIndex - 1, 0, activeTabs.Count - 1)
				;

			if (nextIndex < 0) {
				nextIndex = activeTabs.Count - 1;
			}

			if (lastIndex != nextIndex) {
				ExecuteEvents.Execute(activeTabs[nextIndex].gameObject, new PointerEventData(m_PlayerContext.EventSystem), ExecuteEvents.pointerClickHandler);
				// Button.onClick.Invoke(); // This will ignore disabled state.
			}
		}

	}
}