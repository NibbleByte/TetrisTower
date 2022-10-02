using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
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
		public Button[] Tabs;

		[Tooltip("Contents to be displayed when corresponding tab is active.")]
		public GameObject[] Contents;

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

		public GameObject GetContentByTab(Button tabButton) => Contents[Array.IndexOf(Tabs, tabButton)];

		private int m_CurrentIndex = -1;

		protected virtual void Awake()
		{
			Button disabledTab = Tabs.FirstOrDefault(b => !b.interactable && b.gameObject.activeInHierarchy);

			for (int i = 0; i < Tabs.Length; ++i) {
				Button tabButton = Tabs[i];
				tabButton.onClick.AddListener(() => SwitchToTab(tabButton));
				tabButton.interactable = true;
				Contents[i].SetActive(false);
			}

			if (StartTab == null || !StartTab.gameObject.activeInHierarchy) {
				StartTab = disabledTab ?? Tabs.First(b => b.gameObject.activeInHierarchy);
			}

			// If on, OnEnable will do the same thing.
			if (!ActivateInitialTabOnEnable) {
				SwitchToTab(StartTab);
			}

			NextHotkey?.OnAction.AddListener(OnNextAction);
			PreviousHotkey?.OnAction.AddListener(OnPreviousAction);
		}

		protected virtual void OnEnable()
		{
			if (ActivateInitialTabOnEnable) {
				m_CurrentIndex = -1;	// To skip TabClosed event.

				if (StartTab.gameObject.activeInHierarchy) {
					SwitchToTab(StartTab);
				} else {
					SwitchToTab(Tabs.First(b => b.gameObject.activeInHierarchy));
				}
			}
		}

		protected virtual void OnValidate()
		{
			if (Tabs.Length == 0) {
				Debug.LogError($"\"{name}\" has no Tabs buttons specified.", this);
				return;
			}

			if (Contents.Length == 0) {
				Debug.LogError($"\"{name}\" has no Contents objects specified.", this);
				return;
			}

			if (Tabs.Length != Contents.Length) {
				Debug.LogError($"\"{name}\" Tabs buttons {Tabs.Length} count is different from the Contents objects {Contents.Length}. Every tab button needs corresponding content object.", this);
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
			SwitchToTab(Array.IndexOf(Tabs, tabButton));
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
			List<Button> activeTabs = Tabs.Where(b => b.gameObject.activeInHierarchy).ToList();
			if (activeTabs.Count == 0) {
				Debug.LogError($"{nameof(TabsGroup)} \"{name}\" is trying to switch tab, but non are active!", this);
				return;
			}

			int lastIndex = activeTabs.IndexOf(ActiveTab);

			int nextIndex = WrapAround
				? (lastIndex + 1) % activeTabs.Count
				: Mathf.Clamp(lastIndex + 1, 0, activeTabs.Count - 1)
				;

			if (lastIndex != nextIndex) {
				activeTabs[nextIndex].onClick.Invoke();
			}
		}

		protected virtual void OnPreviousAction()
		{
			List<Button> activeTabs = Tabs.Where(b => b.gameObject.activeInHierarchy).ToList();
			if (activeTabs.Count == 0) {
				Debug.LogError($"{nameof(TabsGroup)} \"{name}\" is trying to switch tab, but non are active!", this);
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
				activeTabs[nextIndex].onClick.Invoke();
			}
		}

	}
}