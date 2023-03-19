using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.UIScope;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Generates some wiki entries.
	/// </summary>
	public class SampleUITesterWikiController : MonoBehaviour
	{
		public Button WikiEntryTab;
		public GameObject WikiEntryContent;

		public TabsGroup TabsGroup;
		public ScrollRect TabsScrollRect;

		public int WikiEntriesCount = 10;

		// Used for multiple event systems (e.g. split screen).
		protected IPlayerContext m_PlayerContext;

		private void Awake()
		{
			m_PlayerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);
		}

		public void Start()
		{
			List<Button> tabs = new List<Button>();
			List<GameObject> contents = new List<GameObject>();

			Transform wikiTabsParent = WikiEntryTab.transform.parent;
			Transform wikiContentsParent = WikiEntryContent.transform.parent;
			for(int i = 0; i < WikiEntriesCount; ++i) {
				GameObject tabGO = GameObject.Instantiate(WikiEntryTab.gameObject, wikiTabsParent);
				GameObject contentGO = GameObject.Instantiate(WikiEntryContent.gameObject, wikiContentsParent);

				if (tabGO.GetComponentInChildren<Text>(true)) tabGO.GetComponentInChildren<Text>(true).text = $"Entry {i}";
				if (contentGO.GetComponentInChildren<Text>(true)) contentGO.GetComponentInChildren<Text>(true).text = $"Some content {i}";

#if USE_TEXT_MESH_PRO
				if (tabGO.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)) tabGO.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = $"Entry {i}";
				if (contentGO.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)) contentGO.GetComponentInChildren<TMPro.TextMeshProUGUI>(true).text = $"Some content {i}";
#endif

				tabs.Add(tabGO.GetComponent<Button>());
				contents.Add(contentGO);
			}

			TabsGroup.Tabs = tabs.ToArray();
			TabsGroup.Contents = contents.ToArray();
			TabsGroup.Reinitialize(tabs[0]);

			GameObject.DestroyImmediate(WikiEntryTab.gameObject);
			GameObject.DestroyImmediate(WikiEntryContent.gameObject);
		}

		public void OnTabOpened(Button tabButton)
		{
			// TODO: This doesn't work with PlayerInput component - returns only keyboard, even when clicking with mouse :(
			// Don't snap when user is clicking with a pointer device.
			if (m_PlayerContext.InputContext.GetLastUsedInputDevice() is Mouse)
				return;

			// Copied from https://stackoverflow.com/a/50191835
			Vector2 contentPos = GetSnapToPositionToBringChildIntoView(TabsScrollRect, tabButton.GetComponent<RectTransform>());
			contentPos.x = 0; // HACK: Calculations are a bit off. Dunno why.
			TabsScrollRect.content.localPosition = contentPos;
		}

		public static Vector2 GetSnapToPositionToBringChildIntoView(ScrollRect instance, RectTransform child)
		{
			Canvas.ForceUpdateCanvases();
			Vector2 viewportLocalPosition = instance.viewport.localPosition;
			Vector2 childLocalPosition = child.localPosition;
			Vector2 result = new Vector2(
				0 - (viewportLocalPosition.x + childLocalPosition.x),
				0 - (viewportLocalPosition.y + childLocalPosition.y)
			);
			return result;
		}
	}

}