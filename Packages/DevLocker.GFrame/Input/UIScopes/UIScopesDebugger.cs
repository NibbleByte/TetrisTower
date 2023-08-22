#if USE_INPUT_SYSTEM
#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.Input.UIInputDisplay;
using UnityEngine.UI;

namespace DevLocker.GFrame.Input.UIScope
{
	internal class UIScopesDebugger : EditorWindow
	{
		private UIScopeTreeElement m_RootElement;
		private bool m_ShowInactiveScopes = false;
		private UIScopeDebugUtils.DisplayHotkeyType m_DisplayHotkeys = UIScopeDebugUtils.DisplayHotkeyType.HideInputDetails;
		private bool m_FocusedScopeWasDrawn = false;

		private Vector2 m_ScrollView = Vector2.zero;

		private StringBuilder m_HotkeyNamesBuilder = new StringBuilder();

		[NonSerialized]
		private GUIStyle UrlStyle;
		private GUIStyle WarningStyle;

		private GUIStyle DisabledStyle;
		private GUIStyle InactiveStyle;
		private GUIStyle ActiveStyle;
		private GUIStyle FocusedStyle;

		private GUIContent EventSystemButtonContent;
		private GUIContent FocusButtonContent;
		private GUIContent SelectionControllerButtonContent;

		private ForceInputDevice m_ForceInputDevice;
		private InputBindingDisplayAsset m_ForcedDisplayAsset;

		[MenuItem("Tools/GFrame/UIScopes Debugger")]
		internal static void Init()
		{
			var window = GetWindow<UIScopesDebugger>(false, "UIScopes Debugger");
			window.position = new Rect(window.position.xMin + 100f, window.position.yMin + 100f, 450f, 600f);
			//window.minSize = new Vector2(300f, 400f);

			window.BuildScopesTree();
		}

		private void InitStyles()
		{
			UrlStyle = new GUIStyle(GUI.skin.label);
			UrlStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1.00f, 0.65f, 0.00f) : Color.blue;
			UrlStyle.hover.textColor = UrlStyle.normal.textColor;
			UrlStyle.active.textColor = Color.red;

			WarningStyle = new GUIStyle(UrlStyle);
			WarningStyle.fontStyle = FontStyle.Bold;
			WarningStyle.alignment = TextAnchor.MiddleCenter;

			DisabledStyle = new GUIStyle(UrlStyle);
			DisabledStyle.normal.textColor = DisabledStyle.hover.textColor = Color.gray;

			InactiveStyle = new GUIStyle(UrlStyle);
			InactiveStyle.normal.textColor = InactiveStyle.hover.textColor = UrlStyle.normal.textColor / 1.3f;

			ActiveStyle = new GUIStyle(UrlStyle);

			FocusedStyle = new GUIStyle(UrlStyle);
			FocusedStyle.normal.textColor = FocusedStyle.hover.textColor = Color.green;

			FocusButtonContent = EditorGUIUtility.IconContent("Animation.FilterBySelection");
			EventSystemButtonContent = EditorGUIUtility.IconContent("EventSystem Icon");

			SelectionControllerButtonContent = AssetDatabase.FindAssets($"t:Script {nameof(SelectionController)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.GetCachedIcon)
				.Select(t => new GUIContent(t))
				.FirstOrDefault()
				;
		}

		void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			EditorSceneManager.sceneOpened += OnSceneOpened;
			EditorSceneManager.sceneClosed += OnSceneClosed;
			PrefabStage.prefabStageOpened += OnPrefabStageChanged;
			PrefabStage.prefabStageClosing += OnPrefabStageChanged;
		}

		void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			EditorSceneManager.sceneOpened -= OnSceneOpened;
			EditorSceneManager.sceneClosed -= OnSceneClosed;
			PrefabStage.prefabStageOpened -= OnPrefabStageChanged;
			PrefabStage.prefabStageClosing -= OnPrefabStageChanged;

			if (m_ForceInputDevice) {
				m_ForceInputDevice.ForcedDevice = null;
			}
		}

		void OnGUI()
		{
			if (UrlStyle == null) {
				InitStyles();
			}


			if (m_RootElement == null) {
				// Root element is set to null if scene just loaded. Wait for any other scenes loading...
				for (int i = 0; i < SceneManager.sceneCount; ++i) {
					if (!SceneManager.GetSceneAt(i).isLoaded) {
						EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * 2);
						EditorGUILayout.LabelField("... waiting for scenes to load ...", WarningStyle, GUILayout.ExpandWidth(true));
						Repaint();
						return;
					}
				}
			}

			if (m_RootElement == null) {
				BuildScopesTree();
			}

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUI.BeginChangeCheck();

				Selectable selected = (EventSystem.current?.currentSelectedGameObject != null) ? EventSystem.current?.currentSelectedGameObject.GetComponent<Selectable>() : null;

				Color prevColor = GUI.backgroundColor;
				GUI.backgroundColor = selected && (!selected.IsInteractable() || !selected.isActiveAndEnabled)? Color.red : prevColor;

				selected = (Selectable) EditorGUILayout.ObjectField("Selected Object", selected, typeof(Selectable), true);

				GUI.backgroundColor = prevColor;

				if (EventSystem.current && EditorGUI.EndChangeCheck()) {
					EventSystem.current.SetSelectedGameObject(selected?.gameObject);
				}

				if (GUILayout.Button(SelectionControllerButtonContent, EditorStyles.label, GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
					if (Application.isPlaying) {
						Selection.activeObject = SelectionController.GetActiveInstanceFor(PlayerContextUIRootObject.GlobalPlayerContext);
					} else {
						Selection.activeObject = GameObject.FindObjectOfType<SelectionController>(true);
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				UIScope focusedScope = Application.isPlaying
					? UIScope.FocusedScope(PlayerContextUIRootObject.GlobalPlayerContext)
					: null
					;

				EditorGUILayout.ObjectField("Focused Scope:", focusedScope, typeof(UIScope), true);

				GUILayout.Space(19);
			}
			EditorGUILayout.EndHorizontal();


			EditorGUILayout.BeginHorizontal();
			{
				m_ForcedDisplayAsset = (InputBindingDisplayAsset) EditorGUILayout.ObjectField("Force Input Device", Application.isPlaying && m_ForceInputDevice ? m_ForceInputDevice.ForcedDevice : m_ForcedDisplayAsset, typeof(InputBindingDisplayAsset), false);

				if (Application.isPlaying) {
					if (m_ForcedDisplayAsset) {
						if (m_ForceInputDevice == null) {
							m_ForceInputDevice = GameObject.FindObjectOfType<ForceInputDevice>();

							if (m_ForceInputDevice == null) {
								m_ForceInputDevice = EventSystem.current?.gameObject.AddComponent<ForceInputDevice>();
							}
						}

						if (m_ForceInputDevice) {
							m_ForceInputDevice.ForcedDevice = m_ForcedDisplayAsset;
						}
					} else if (m_ForceInputDevice) {
						m_ForceInputDevice.ForcedDevice = null;
					}
				}

				if (GUILayout.Button(EventSystemButtonContent, EditorStyles.label, GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
					if (Application.isPlaying) {
						Selection.activeGameObject = EventSystem.current?.gameObject;
					} else {
						Selection.activeObject = GameObject.FindObjectOfType<EventSystem>(true)?.gameObject;
					}
				}
			}
			EditorGUILayout.EndHorizontal();



			EditorGUILayout.BeginHorizontal();
			{
				m_ShowInactiveScopes = EditorGUILayout.Toggle("Show Inactive Scopes", m_ShowInactiveScopes, GUILayout.Width(16));

				m_DisplayHotkeys = (UIScopeDebugUtils.DisplayHotkeyType) EditorGUILayout.EnumPopup(" ", m_DisplayHotkeys);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Rescan")) {
					BuildScopesTree();
				}
			}
			EditorGUILayout.EndHorizontal();

			m_ScrollView = GUILayout.BeginScrollView(m_ScrollView);

			m_FocusedScopeWasDrawn = false;
			DrawScopes(m_RootElement);

			GUILayout.EndScrollView();

			// Do only on repaint or exceptions happen.
			if (Application.isPlaying && Event.current != null && Event.current.type == EventType.Repaint && !m_FocusedScopeWasDrawn && UIScope.FocusedScope(PlayerContextUIRootObject.GlobalPlayerContext)) {
				m_RootElement = null;
				GUIUtility.ExitGUI();
			}

			// Don't refresh every frame.
			if (Application.isPlaying && !EditorApplication.isPaused) {
				Repaint();
			}
		}

		private void BuildScopesTree()
		{
			m_RootElement = new UIScopeTreeElement() { Foldout = true };

			PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

			try {
				if (prefabStage == null) {

					UIScopeDebugUtils.GatherScopes(m_RootElement, GatherScopesIterator);

				} else {

					EditorUtility.DisplayCancelableProgressBar("UIScopes scan", $"Searching \"{prefabStage.prefabContentsRoot.name}\"...", 0f);

					UIScopeDebugUtils.GatherScopes(m_RootElement, prefabStage.prefabContentsRoot.transform, GatherScopesIterator);
				}
			}
			finally {
				EditorUtility.ClearProgressBar();
				Repaint();
			}
		}

		private void GatherScopesIterator(Transform transform)
		{
			if (m_ForceInputDevice == null) {
				m_ForceInputDevice = transform.GetComponent<ForceInputDevice>();
			}
		}

		private void DrawScopes(UIScopeTreeElement element)
		{
			if (element.Foldout) {
				foreach (UIScopeTreeElement child in element.Children) {

					UIScope scope = child.Scope;
					int depth = child.Depth;
					bool scopeIsEnabled = scope && scope.isActiveAndEnabled;

					if (Application.isPlaying && scope == UIScope.FocusedScope(PlayerContextUIRootObject.GlobalPlayerContext)) {
						m_FocusedScopeWasDrawn = true;
					}

					if (!m_ShowInactiveScopes && !scopeIsEnabled)
						continue;

					EditorGUILayout.BeginHorizontal();

					// Because EditorGUI.indentLevel doesn't work with buttons and had to calculate rects manually.
					float indent = depth * 10f;
					GUILayout.Space(indent);

					var foldOutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(12f));
					if (child.Children.Count > 0) {
						EditorGUI.BeginChangeCheck();
						child.Foldout = EditorGUI.Foldout(foldOutRect, child.Foldout, "");

						if (EditorGUI.EndChangeCheck() && Event.current.alt) {
							SetFoldoutRecursively(child, child.Foldout);
						}
					}

					bool scopeInactiveOrDisabled;
					GUIStyle scopeStyle;
					if (!Application.isPlaying) {
						scopeStyle = scopeIsEnabled ? UrlStyle : DisabledStyle;
						scopeInactiveOrDisabled = true;
					} else if (scope == null || !scopeIsEnabled) {
						scopeStyle = DisabledStyle;
						scopeInactiveOrDisabled = true;
					} else if (scope.IsFocused) {
						scopeStyle = FocusedStyle;
						scopeInactiveOrDisabled = false;
					} else if (scope.IsActive) {
						scopeStyle = ActiveStyle;
						scopeInactiveOrDisabled = false;
					} else {
						scopeStyle = InactiveStyle;
						scopeInactiveOrDisabled = true;
					}

					string scopeLabel;
					m_HotkeyNamesBuilder.Clear();

					if (scope) {
						IInputContext context = null;
						var scopeElements = new List<IScopeElement>();

						if (Application.isPlaying) {
							scopeElements = scope.OwnedElements.ToList();
							context = scope.PlayerContext?.InputContext;
						} else {
							var directChildScopes = new List<UIScope>();
							UIScope.ScanForOwnedScopeElements(scope, scope.transform, scopeElements, directChildScopes);
						}

						int enabledElementsCount = !scopeInactiveOrDisabled ? scopeElements.Count(e => {
							var behaviour = e as MonoBehaviour;
							return behaviour == null || (e.enabled && behaviour.isActiveAndEnabled);
						}) : 0;

						string elementsStatus = (enabledElementsCount == scopeElements.Count || scopeInactiveOrDisabled || !Application.isPlaying)
							? "[Ok]"
							: $"[{enabledElementsCount} / {scopeElements.Count}]"
							;

						scopeLabel = $"\"{scope.name}\" {elementsStatus}";



						if (m_DisplayHotkeys != UIScopeDebugUtils.DisplayHotkeyType.HideInputDetails) {
							List<InputAction> hotkeys = new List<InputAction>();

							foreach (var hotkeyElement in scopeElements.OfType<IHotkeysWithInputActions>()) {
								foreach (InputAction inputAction in hotkeyElement.GetUsedActions(context)) {
									if (!hotkeys.Contains(inputAction)) {
										hotkeys.Add(inputAction);
									}
								}
							}

							InputControlScheme currentScheme;
							if (context != null) {
								currentScheme = context.GetLastUsedInputControlScheme();
							} else if (m_ForcedDisplayAsset) {
								currentScheme = new InputControlScheme() { bindingGroup = m_ForcedDisplayAsset.MatchingControlScheme };
							}
							InputBinding matchBinding = new InputBinding() { groups = currentScheme.bindingGroup };

							string separator = m_DisplayHotkeys == UIScopeDebugUtils.DisplayHotkeyType.DisplayInputBindings ? ", " : " | ";
							m_HotkeyNamesBuilder.AppendJoin(separator, UIScopeDebugUtils.GetHotkeyNames(context, hotkeys, matchBinding, m_DisplayHotkeys));
						}

					} else {
						scopeLabel = "<< Missing >>";
					}

					EditorGUI.BeginDisabledGroup(scope == null || !Application.isPlaying);
					if (GUILayout.Button(FocusButtonContent, EditorStyles.label, GUILayout.ExpandWidth(false))) {
						scope.Focus();
					}
					EditorGUI.EndDisabledGroup();

					if (GUILayout.Button(scopeLabel, scopeStyle, GUILayout.MaxWidth(180f - indent), GUILayout.ExpandWidth(false)) && scope) {
						EditorGUIUtility.PingObject(scope);
					}

					GUILayout.Space(8f);

					if (m_DisplayHotkeys != UIScopeDebugUtils.DisplayHotkeyType.HideInputDetails) {
						string hotkeyLabel = m_HotkeyNamesBuilder.ToString();
						if (!string.IsNullOrEmpty(hotkeyLabel)) {
							EditorGUILayout.TextField(hotkeyLabel);
						}
					}

					EditorGUILayout.EndHorizontal();

					DrawScopes(child);
				}
			}
		}


		private void SetFoldoutRecursively(UIScopeTreeElement element, bool foldout)
		{
			element.Foldout = foldout;

			foreach(UIScopeTreeElement child in element.Children) {
				SetFoldoutRecursively(child, foldout);
			}
		}

		#region Event handlers for refresh

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch (state) {
				case PlayModeStateChange.EnteredEditMode:
				case PlayModeStateChange.EnteredPlayMode:
					m_RootElement = null;
					Repaint();
					break;
			}
		}

		private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			m_RootElement = null;
			Repaint();
		}

		private void OnSceneUnloaded(Scene arg0)
		{
			m_RootElement = null;
			Repaint();
		}

		private void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			m_RootElement = null;
			Repaint();
		}

		private void OnSceneClosed(Scene scene)
		{
			m_RootElement = null;
			Repaint();
		}

		private void OnPrefabStageChanged(PrefabStage obj)
		{
			m_RootElement = null;
			Repaint();
		}

		#endregion

	}
}

#endif
#endif