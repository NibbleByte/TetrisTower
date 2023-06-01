#if USE_INPUT_SYSTEM
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace DevLocker.GFrame.Input.UIScope
{
	internal class UIScopesDebugger : EditorWindow
	{
		private class UIScopeTreeElement
		{
			public UIScope Scope;
			public int Depth = 0;
			public List<UIScopeTreeElement> Children = new List<UIScopeTreeElement>();

			public bool Foldout = true;
		}

		private UIScopeTreeElement m_RootElement;
		private bool m_ShowHotkeys = true;

		private Vector2 m_ScrollView = Vector2.zero;

		private StringBuilder m_HotkeyNamesBuilder = new StringBuilder();

		private GUIStyle UrlStyle;

		private GUIStyle DisabledStyle;
		private GUIStyle InactiveStyle;
		private GUIStyle ActiveStyle;
		private GUIStyle FocusedStyle;

		private GUIContent EventSystemButtonContent;
		private GUIContent FocusButtonContent;

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

			DisabledStyle = new GUIStyle(UrlStyle);
			DisabledStyle.normal.textColor = DisabledStyle.hover.textColor = Color.gray;

			InactiveStyle = new GUIStyle(UrlStyle);
			InactiveStyle.normal.textColor = InactiveStyle.hover.textColor = UrlStyle.normal.textColor / 1.3f;

			ActiveStyle = new GUIStyle(UrlStyle);

			FocusedStyle = new GUIStyle(UrlStyle);
			FocusedStyle.normal.textColor = FocusedStyle.hover.textColor = Color.green;

			FocusButtonContent = EditorGUIUtility.IconContent("Animation.FilterBySelection");
			EventSystemButtonContent = EditorGUIUtility.IconContent("EventSystem Icon");
		}

		void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		void OnGUI()
		{
			if (UrlStyle == null) {
				InitStyles();

			}

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			{
				EditorGUILayout.ObjectField("Selected Object", EventSystem.current?.currentSelectedGameObject, typeof(GameObject), true);
				if (GUILayout.Button(EventSystemButtonContent, EditorStyles.label, GUILayout.Width(16), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
					Selection.activeGameObject = EventSystem.current?.gameObject;
				}

			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();



			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Rescan") || m_RootElement == null) {
					BuildScopesTree();
				}

				m_ShowHotkeys = EditorGUILayout.Toggle(m_ShowHotkeys, GUILayout.Width(16));
			}
			EditorGUILayout.EndHorizontal();

			m_ScrollView = GUILayout.BeginScrollView(m_ScrollView);

			DrawScopes(m_RootElement);

			GUILayout.EndScrollView();

			if (Application.isPlaying) {
				Repaint();
			}
		}

		private void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			switch(state) {
				case PlayModeStateChange.EnteredEditMode:
				case PlayModeStateChange.EnteredPlayMode:
					BuildScopesTree();
					break;
			}
		}

		private void BuildScopesTree()
		{
			m_RootElement = new UIScopeTreeElement() { Foldout = true };

			PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

			try {
				if (prefabStage == null) {
					List<GameObject> allObjects = new List<GameObject>();

					for (int i = 0; i < SceneManager.sceneCount; ++i) {
						allObjects.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());
					}


					for (int i = 0; i < allObjects.Count; ++i) {

						if (EditorUtility.DisplayCancelableProgressBar("UIScopes scan", $"Searching \"{allObjects[i].name}\"...", (float) i / allObjects.Count))
							break;

						GatherScopes(m_RootElement, allObjects[i].transform);
					}

				} else {

					EditorUtility.DisplayCancelableProgressBar("UIScopes scan", $"Searching \"{prefabStage.prefabContentsRoot.name}\"...", 0f);

					GatherScopes(m_RootElement, prefabStage.prefabContentsRoot.transform);
				}
			}
			finally {
				EditorUtility.ClearProgressBar();
				Repaint();
			}
		}

		private void GatherScopes(UIScopeTreeElement element, Transform transform)
		{
			UIScope scope = transform.GetComponent<UIScope>();
			if (scope) {
				element.Children.Add(new UIScopeTreeElement() { Scope = scope, Depth = element.Depth + 1 });
				element = element.Children.Last();
			}

			foreach(Transform child in transform) {
				GatherScopes(element, child);
			}
		}

		private void DrawScopes(UIScopeTreeElement element)
		{
			if (element.Foldout) {
				foreach (UIScopeTreeElement child in element.Children) {

					UIScope scope = child.Scope;
					int depth = child.Depth;

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
						scopeStyle = UrlStyle;
						scopeInactiveOrDisabled = true;
					} else if (scope == null || !scope.isActiveAndEnabled) {
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

						var scopeElements = new List<IScopeElement>();
						if (Application.isPlaying) {
							scopeElements = scope.OwnedElements.ToList();
						} else {
							var directChildScopes = new List<UIScope>();
							UIScope.ScanForOwnedScopeElements(scope, scope.transform, scopeElements, directChildScopes);
						}

						int enabledElementsCount = !scopeInactiveOrDisabled ? scopeElements.Count(e => {
							var behaviour = e as MonoBehaviour;
							return e.enabled && (behaviour == null || behaviour.isActiveAndEnabled);
						}) : 0;

						string elementsStatus = (enabledElementsCount == scopeElements.Count || scopeInactiveOrDisabled || !Application.isPlaying)
							? "[Ok]"
							: $"[{enabledElementsCount} / {scopeElements.Count}]"
							;

						scopeLabel = $"\"{scope.name}\" {elementsStatus}";


						if (scope.m_PlayerContext?.InputContext != null) {
							List<InputAction> hotkeys = new List<InputAction>();

							foreach (var hotkeyElement in scopeElements.OfType<IHotkeysWithInputActions>()) {
								foreach(InputAction inputAction in hotkeyElement.GetUsedActions(scope.m_PlayerContext.InputContext)) {
									if (!hotkeys.Contains(inputAction)) {
										hotkeys.Add(inputAction);
									}
								}
							}

							m_HotkeyNamesBuilder.AppendJoin(' ', hotkeys.Select(a => $"[{a.name}] "));
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

					if (m_ShowHotkeys) {
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
	}
}

#endif
#endif