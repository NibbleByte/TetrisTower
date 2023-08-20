using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Helper class to build <see cref="UIScope"/> trees. Useful for debug tools.
	/// </summary>
	public class UIScopeTreeElement
	{
		public UIScope Scope;
		public int Depth = 0;
		public List<UIScopeTreeElement> Children = new List<UIScopeTreeElement>();

		public bool Foldout = true;
	}

	/// <summary>
	/// Useful functions for debug tools.
	/// </summary>
	public static class UIScopeDebugUtils
	{
		public enum DisplayHotkeyType
		{
			HideInputDetails,
			DisplayInputActions,
			DisplayInputBindings,
			DisplayInputBoth,
		}

		public static string GetScopesInfo(DisplayHotkeyType displayHotkeys = DisplayHotkeyType.HideInputDetails, bool includeDisabled = false, InputControlScheme schemeMask = default)
		{
			var output = new StringBuilder();
			var root = new UIScopeTreeElement() { Foldout = true };

			GatherScopes(root);

			GetScopesInfo(root, displayHotkeys, includeDisabled,schemeMask, output);

			return output.ToString();
		}

		private static void GetScopesInfo(UIScopeTreeElement element, DisplayHotkeyType displayHotkeys, bool includeDisabled, InputControlScheme schemeMask, StringBuilder output)
		{
			foreach (UIScopeTreeElement child in element.Children) {
				UIScope scope = child.Scope;
				int depth = child.Depth;
				bool scopeIsEnabled = scope && scope.isActiveAndEnabled;

				if (scope == null)
					continue;

				if (!includeDisabled && !scopeIsEnabled)
					continue;

				const string FocusedStatus = "F";
				const string ActiveStatus = "A";
				const string InactiveStatus = "I";
				const string DisabledStatus = "D";

				string scopeStatus;
				if (!Application.isPlaying) {
					scopeStatus = scopeIsEnabled ? ActiveStatus : InactiveStatus;
				} else if (scope == null || !scopeIsEnabled) {
					scopeStatus = DisabledStatus;
				} else if (scope.IsFocused) {
					scopeStatus = FocusedStatus;
				} else if (scope.IsActive) {
					scopeStatus = ActiveStatus;
				} else {
					scopeStatus = InactiveStatus;
				}

				var scopeElements = new List<IScopeElement>();
				if (Application.isPlaying) {
					scopeElements = scope.OwnedElements.ToList();
				} else {
					var directChildScopes = new List<UIScope>();
					UIScope.ScanForOwnedScopeElements(scope, scope.transform, scopeElements, directChildScopes);
				}

				int enabledElementsCount = scopeElements.Count(e => {
					var behaviour = e as MonoBehaviour;
					return behaviour == null || (e.enabled && behaviour.isActiveAndEnabled);
				});


				output.Append("".PadRight(child.Depth * 2, ' '));
				output.Append($"> {scopeStatus} \"{scope.name}\" [{enabledElementsCount} / {scopeElements.Count}]");


				if (displayHotkeys != DisplayHotkeyType.HideInputDetails) {
					List<InputAction> hotkeys = new List<InputAction>();

					foreach (var hotkeyElement in scopeElements.OfType<IHotkeysWithInputActions>()) {
						foreach (InputAction inputAction in hotkeyElement.GetUsedActions(scope.PlayerContext?.InputContext)) {
							if (!hotkeys.Contains(inputAction)) {
								hotkeys.Add(inputAction);
							}
						}
					}

					InputBinding matchBinding = new InputBinding() { groups = schemeMask.bindingGroup };

					string separator = displayHotkeys == DisplayHotkeyType.DisplayInputBindings ? ", " : " | ";
					output.Append(" => ");
					output.AppendJoin(separator, GetHotkeyNames(hotkeys, matchBinding, displayHotkeys));
				}

				output.AppendLine();

				GetScopesInfo(child, displayHotkeys, includeDisabled, schemeMask, output);
			}
		}

		public static IEnumerable<string> GetHotkeyNames(IEnumerable<InputAction> actions, InputBinding matchBinding, DisplayHotkeyType displayType)
		{
			switch (displayType) {
				case DisplayHotkeyType.DisplayInputActions:
					foreach (InputAction action in actions) {
						yield return $"{action.name}";
					}
					break;

				case DisplayHotkeyType.DisplayInputBindings:
					foreach (InputAction action in actions) {
						for (int i = 0; i < action.bindings.Count; ++i) {
							InputBinding binding = action.bindings[i];

							if (binding.isComposite) {
								if (i < action.bindings.Count - 1 && !matchBinding.Matches(action.bindings[i + 1]))
									continue;

							} else if (!matchBinding.Matches(binding) || binding.isPartOfComposite) {
								continue;
							}

							string bindingName = GetBindingNameToDisplay(action, i);
							if (string.IsNullOrWhiteSpace(bindingName))
								continue;

							yield return bindingName;
						}
					}
					break;

				case DisplayHotkeyType.DisplayInputBoth:

					List<string> bindingNames = new List<string>();

					foreach (InputAction action in actions) {
						bindingNames.Clear();

						for (int i = 0; i < action.bindings.Count; ++i) {
							InputBinding binding = action.bindings[i];

							if (binding.isComposite) {
								if (i < action.bindings.Count - 1 && !matchBinding.Matches(action.bindings[i + 1]))
									continue;

							} else if (!matchBinding.Matches(binding) || binding.isPartOfComposite) {
								continue;
							}

							string bindingName = GetBindingNameToDisplay(action, i);
							if (string.IsNullOrWhiteSpace(bindingName))
								continue;

							bindingNames.Add(bindingName);
						}

						if (bindingNames.Count > 0) {
							yield return $"{action.name} = {string.Join(", ", bindingNames)}";
						} else {
							yield return $"{action.name}";
						}
					}
					break;

				default:
					throw new ArgumentException(displayType.ToString());
			}
		}

		private static string GetBindingNameToDisplay(InputAction action, int bindingIndex)
		{
			string bname;
			try {
				bname = action.GetBindingDisplayString(bindingIndex);
			}
			catch (NotImplementedException) {
				// HACK: current version of the InputSystem 1.3.0 doesn't support texts for special bindings like "*/{Submit}".
				// This is what they say in the MatchControlsRecursive():
				////TODO: support scavenging a subhierarchy for usages
				//throw new NotImplementedException("Matching usages inside subcontrols instead of at device root");
				bname = action.bindings[bindingIndex].ToDisplayString();
			}

			if (bname.Length > 2 || bname.Contains(' ') || bname.Contains('/'))
				bname = $"[{bname}]";
			return bname;
		}

		/// <summary>
		/// Pass in empty root element to append all scopes in the currently loaded scenes (including DontDestroyOnLoaded).
		/// </summary>
		public static void GatherScopes(UIScopeTreeElement root, Action<Transform> iterator = null)
		{
			List<GameObject> allObjects = new List<GameObject>();

			for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i) {
				allObjects.AddRange(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects());
			}

			allObjects.AddRange(GetDontDestroyOnLoadObjects());

			try {
				for (int i = 0; i < allObjects.Count; ++i) {

#if UNITY_EDITOR
					if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("UIScopes scan", $"Searching \"{allObjects[i].name}\"...", (float)i / allObjects.Count))
						break;
#endif


					GatherScopes(root, allObjects[i].transform, iterator);
				}
			}
			finally {
#if UNITY_EDITOR
				UnityEditor.EditorUtility.ClearProgressBar();
#endif
			}
		}

		/// <summary>
		/// Pass in empty root element to append all scopes under specified transform.
		/// </summary>
		public static void GatherScopes(UIScopeTreeElement root, Transform transform, Action<Transform> iterator = null)
		{
			GatherScopes(root, root, transform, iterator);
		}

		private static void GatherScopes(UIScopeTreeElement root, UIScopeTreeElement element, Transform transform, Action<Transform> iterator = null)
		{
			iterator?.Invoke(transform);

			UIScope scope = transform.GetComponent<UIScope>();
			if (scope) {
				if (scope.Type >= UIScope.ScopeType.Root) {
					element = root;
				}

				element.Children.Add(new UIScopeTreeElement() { Scope = scope, Depth = element.Depth + 1 });
				element = element.Children.Last();
			}

			foreach (Transform child in transform) {
				GatherScopes(root, element, child, iterator);
			}
		}

		// Turns out DontDestroyOnLoad scene is not included in the SceneManager.
		// https://forum.unity.com/threads/editor-script-how-to-access-objects-under-dontdestroyonload-while-in-play-mode.442014/#post-3570916
		private static GameObject[] GetDontDestroyOnLoadObjects()
		{
			if (!Application.isPlaying)
				return Array.Empty<GameObject>();

			GameObject temp = null;
			try {
				temp = new GameObject();
				UnityEngine.Object.DontDestroyOnLoad(temp);
				var dontDestroyOnLoad = temp.scene;
				UnityEngine.Object.Destroy(temp);
				temp = null;

				return dontDestroyOnLoad.GetRootGameObjects();
			}
			finally {
				if (temp != null)
					UnityEngine.Object.Destroy(temp);
			}
		}
	}
}