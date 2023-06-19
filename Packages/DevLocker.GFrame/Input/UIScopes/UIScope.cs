using DevLocker.GFrame.Input.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevLocker.GFrame.Input.UIScope
{
	/// <summary>
	/// Marks scope elements to be controlled by the UIScope.
	/// </summary>
	public interface IScopeElement
	{
		bool enabled { get; set; }
	}

#if USE_INPUT_SYSTEM
	/// <summary>
	/// Used for displaying InputActions state for debugging.
	/// </summary>
	public interface IHotkeysWithInputActions
	{
		IEnumerable<UnityEngine.InputSystem.InputAction> GetUsedActions(IInputContext inputContext);
	}

	/// <summary>
	/// Implement this if you want to be able to set current hotkey in a generic matter. Used for editor UI.
	/// </summary>
	public interface IWritableHotkeyInputActionReference
	{
		void SetInputAction(UnityEngine.InputSystem.InputActionReference inputActionReference);
	}
#endif

	/// <summary>
	/// When working with more hotkeys, selections, etc. on screen, some conflicts may arise.
	/// For example:
	/// > "Back" hotkey element is attached on the displayed menu and another "Back" hotkey attached to the "Yes/No" pop up, displayed on top.
	/// > The usual solution is to invoke only the last enabled hotkey instead of all of them.
	/// UIScope groups all child scope elements into a single group. The last enabled UIScope is focused - it and all its parent scopes are activated, while the rest will be disabled.
	///
	/// Note: Each scope belongs to a <see cref="PlayerScopeSet"/> - each player has it's own set of scopes with it's own navigation, hotkeys, selection etc.
	///		  If there is only one player (i.e. no player root objects), global <see cref="PlayerScopeSet"/> is used.
	///		  For more info check <see cref="PlayerContextUIRootObject"/>.
	/// </summary>
	[SelectionBase]
	public class UIScope : MonoBehaviour
	{
		public enum OnEnablePolicy
		{
			Focus = 0,
			FocusWithFramePriority = 2,	// If multiple activations
			FocusIfCurrentIsLowerDepth = 5,
			FocusIfCurrentIsLowerOrEqualDepth = 7,
			DontFocus = 20,
		}

		public enum OnDisablePolicy
		{
			FocusScopeWithHighestDepth = 0,
			FocusPreviousScope = 5,
			FocusParentScope = 10,
			FocusFirstEnabledScopeFromList = 15,
			EmptyFocus = 20,
		}

		/// <summary>
		/// Contains all scopes used by certain player,
		/// since every player needs to have their own active & focused scopes.
		/// </summary>
		protected class PlayerScopeSet
		{
			public UIScope[] ActiveScopes = Array.Empty<UIScope>();

			public List<UIScope> RegisteredScopes = new List<UIScope>();

			// Switching scopes may trigger user code that may switch scopes indirectly, while already doing so.
			// Any such change will be pushed to a queue and applied later on.
			public bool ChangingActiveScopes = false;
			public Queue<KeyValuePair<UIScope, bool>> PendingScopeChanges = new Queue<KeyValuePair<UIScope, bool>>();
		}

		[Tooltip("Focusing a scope will activate all parent scopes up till the first root or the top one is reached (parent scopes of closest root will remain inactive).")]
		public bool IsRoot = false;

		[Tooltip("If on, you can choose OnEnable and OnDisable automatic focus behaviour.\n\nIf off, this scope will always be active when enabled and vice versa. You'll have to manually handle this and it will be excluded from any focus schemes (i.e. it can be active while another scope is focused + parents). You'll be responsible for managing conflicts.")]
		public bool AutomaticFocus = true;

		[Tooltip("What should happen when this scope gets enabled.\n\nFocusWithFramePriority - use when multiple scopes get enabled in the same frame to prioritize this one.")]
		public OnEnablePolicy OnEnableBehaviour;
		[Tooltip("What should happen when this scope gets disabled ONLY if this is the FOCUSED (deepest) one.")]
		public OnDisablePolicy OnDisableBehaviour;

		public List<UIScope> OnDisableScopes;

#if USE_INPUT_SYSTEM
		[Space]
		[Tooltip("Reset all input actions.\nThis will interrupt their progress and any gesture, drag, sequence will be canceled.")]
		public bool ResetAllActionsOnEnable = true;

		[Tooltip("Use this for modal windows to suppress background hotkeys.\n\nPushes a new input state in the stack.\nOn deactivating, will pop this state and restore the previous one.\nThe only enabled actions will be the used ones by (under) this scope.")]
		public bool PushInputStack = false;
		[Tooltip("Enable the UI actions with the scope ones, after pushing the new input state.")]
		public bool IncludeUIActions = true;
#endif

		public event Action Activated;
		public event Action Focused;

		public event Action Deactivating;
		public event Action Unfocusing;

		public delegate void ScopeEvent(UIScope scope);

		public static event ScopeEvent ScopeActivated;
		public static event ScopeEvent ScopeFocused;

		public static event ScopeEvent ScopeDeactivating;
		public static event ScopeEvent ScopeUnfocusing;

		public UIScope LastFocusedScope => m_LastFocusedScope;
		private UIScope m_LastFocusedScope;

		public int ScopeDepth => m_ScopeDepth;
		private int m_ScopeDepth;

		private int m_FrameEnabled = -1;

		/// <summary>
		/// Focused scope which keeps it and all its parents active (and the rest will be inactive).
		/// </summary>
		/// <param name="playerRoot">Player root the scope is part of (in case of split-screen)</param>
		public static UIScope FocusedScope(PlayerContextUIRootObject playerRoot)
		{
			s_PlayerSets.TryGetValue(playerRoot, out PlayerScopeSet playerSet);

			return playerSet?.ActiveScopes.LastOrDefault();
		}

		/// <summary>
		/// The focused scope plus all it's parents - top to bottom.
		/// <param name="playerRoot">Player root the scope is part of (in case of split-screen)</param>
		/// </summary>
		public static IReadOnlyCollection<UIScope> GetActiveScopes(PlayerContextUIRootObject playerRoot)
		{
			s_PlayerSets.TryGetValue(playerRoot, out PlayerScopeSet playerSet);

			return playerSet?.ActiveScopes;
		}

		/// <summary>
		/// Get all registered scopes (including inactive ones).
		/// <param name="playerRoot">Player root the scopes are part of (in case of split-screen)</param>
		/// </summary>
		public static IReadOnlyCollection<UIScope> GetRegisteredScopes(PlayerContextUIRootObject playerRoot)
		{
			s_PlayerSets.TryGetValue(playerRoot, out PlayerScopeSet playerSet);

			return playerSet?.RegisteredScopes;
		}

		private static Dictionary<PlayerContextUIRootObject, PlayerScopeSet> s_PlayerSets = new Dictionary<PlayerContextUIRootObject, PlayerScopeSet>();

		// Player set used by this UIScope.
		private PlayerScopeSet m_PlayerSet;
		internal PlayerContextUIRootObject m_PlayerContext;

		public IReadOnlyList<IScopeElement> OwnedElements => m_ScopeElements;
		public IReadOnlyList<UIScope> DirectChildScopes => m_DirectChildScopes;

		private List<IScopeElement> m_ScopeElements = new List<IScopeElement>();
		private List<UIScope> m_DirectChildScopes = new List<UIScope>();

		protected bool m_HasInitialized = false;

		private bool m_GameQuitting = false;


		/// <summary>
		/// Called when assembly reload is disabled.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStaticsCache()
		{
			s_PlayerSets.Clear();
		}

		protected virtual void Awake()
		{
			// Can be RootObject or RootForward.
			IPlayerContext playerContext = PlayerContextUtils.GetPlayerContextFor(gameObject);

			playerContext.AddSetupCallback((delayedSetup) => {

				bool result = Initialize();
				if (!result) {
					Debug.LogError($"[Input] UIScope {name} initialize failed, which should be impossible! Delayed setup: {delayedSetup}.");
					return;
				}

				m_HasInitialized = true;

				if (delayedSetup && isActiveAndEnabled) {
					OnEnable();
				}
			});
		}

		void OnApplicationQuit()
		{
			m_GameQuitting = true;
		}

		public virtual void OnValidate()
		{
			// Copy-Paste components have list null.
			if (OnDisableScopes != null) {
				for (int i = 0; i < OnDisableScopes.Count; ++i) {
					if (OnDisableScopes[i] == null) {
						Debug.LogError($"[Input] \"{name}\" has missing scope in {nameof(OnDisableScopes)} list at scene \"{gameObject.scene.name}\"", this);
					}
				}
			}
		}

		public virtual bool Initialize()
		{
			PlayerContextUIRootObject playerContext = PlayerContextUtils.GetPlayerContextFor(gameObject).GetRootObject();

			// If using forwarder, it may not yet be setup. Will do delayed Initialize(). Check Update().
			if (playerContext == null) {
				return false;
			}

			m_PlayerContext = playerContext;

			if (!s_PlayerSets.TryGetValue(playerContext, out m_PlayerSet)) {
				m_PlayerSet = new PlayerScopeSet();
				s_PlayerSets.Add(playerContext, m_PlayerSet);
			}

			m_HasInitialized = true;

			ScanForOwnedScopeElements();

			return true;
		}

		void OnEnable()
		{
			if (!m_HasInitialized)
				return;

			m_FrameEnabled = Time.frameCount;
			m_LastFocusedScope = m_PlayerSet.ActiveScopes.LastOrDefault();

			if (!AutomaticFocus) {
				SetScopeState(true);
				return;
			}

			if (m_PlayerSet.ChangingActiveScopes) {
				m_PlayerSet.PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, true));
				return;
			}

			// Child scope was active, but this one was disabled. The user just enabled me.
			// Re-insert me (us) to the collections keeping the correct order.
			if (m_PlayerSet.ActiveScopes.Length > 0 && m_PlayerSet.ActiveScopes.Last().transform.IsChildOf(transform)) {

				// That would include me, freshly enabled.
				UIScope[] nextScopes = CollectScopes(m_PlayerSet.ActiveScopes.Last());

				for (int i = 0; i < nextScopes.Length; ++i) {
					UIScope scope = nextScopes[i];
					scope.m_ScopeDepth = i; // Always set this in case hierarchy changed in the mean time.
				}

				if (!m_PlayerSet.RegisteredScopes.Contains(this)) {
					m_PlayerSet.RegisteredScopes.Add(this);
				}

				SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);

			} else {

				// OnEnabled() order of execution is undefined - sometimes parent invoked first, sometimes the children.
				// Ensure that collections don't have any duplicates and are filled in the right order - parent to child.
				UIScope[] nextScopes = CollectScopes(this);

				for(int i = 0; i < nextScopes.Length; ++i) {
					UIScope scope = nextScopes[i];
					scope.m_ScopeDepth = i;	// Always set this in case hierarchy changed in the mean time.

					if (!m_PlayerSet.RegisteredScopes.Contains(scope)) {
						m_PlayerSet.RegisteredScopes.Add(scope);
					}
				}

				UIScope lastActive = m_PlayerSet.ActiveScopes.LastOrDefault();

				switch (OnEnableBehaviour) {
					case OnEnablePolicy.Focus:
						// Skip activation if this frame another scope was activated with higher priority.
						if (!m_PlayerSet.ActiveScopes.Any(s => s.m_FrameEnabled == m_FrameEnabled && s.OnEnableBehaviour == OnEnablePolicy.FocusWithFramePriority)) {
							SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
						}
						break;

					case OnEnablePolicy.FocusWithFramePriority:
						SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
						break;

					case OnEnablePolicy.FocusIfCurrentIsLowerDepth:
						if (lastActive == null || lastActive.m_ScopeDepth < m_ScopeDepth) {
							SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
						}
						break;

					case OnEnablePolicy.FocusIfCurrentIsLowerOrEqualDepth:
						if (lastActive == null || lastActive.m_ScopeDepth <= m_ScopeDepth) {
							SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
						}
						break;

					case OnEnablePolicy.DontFocus:
						break;

					default:
						throw new NotSupportedException(OnEnableBehaviour.ToString());
				}
			}
		}

		void OnDisable()
		{
			if (!m_HasInitialized)
				return;

			m_FrameEnabled = -1;

			if (!AutomaticFocus) {
				SetScopeState(false);
				return;
			}

			if (m_PlayerSet.ChangingActiveScopes) {
				m_PlayerSet.PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, false));
				return;
			}

			m_PlayerSet.RegisteredScopes.Remove(this);

			// if this scope got activated and added parents to m_PlayerScope.Scopes, but got immediately deactivated BEFORE the parent got enabled,
			// parent would remain in that collection forever (or until activated). In that case remove the parents from the collection that are not enabled.
			// This could be an issue if sibling scope child of the same parent is active - do this only if parent is actually inactive in the hierarchy.
			// If it is active in the hierarchy, it will eventually get enabled and added to the collection.
			for (int i = m_PlayerSet.RegisteredScopes.Count - 1; i >= 0; --i) {
				if (m_PlayerSet.RegisteredScopes[i].m_FrameEnabled == -1 && !m_PlayerSet.RegisteredScopes[i].gameObject.activeInHierarchy && transform.IsChildOf(m_PlayerSet.RegisteredScopes[i].transform)) {
					m_PlayerSet.RegisteredScopes.RemoveAt(i);
				}
			}

			// HACK: On turning off the game OnDisable() gets called which may call methods on destroyed objects.
			int activeIndex = Array.IndexOf(m_PlayerSet.ActiveScopes, this);
			if (activeIndex != -1 && !m_GameQuitting) {

				// Proceed only if this is the deepest scope (i.e. the focused one).
				if (activeIndex != m_PlayerSet.ActiveScopes.Length - 1) {
					var activeScopes = CollectScopes(m_PlayerSet.ActiveScopes.Last());
					SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, activeScopes);
					return;
				}



				// Something else just activated, use that if appropriate.
				UIScope fallbackFrameScope = null;
				foreach(UIScope scope in m_PlayerSet.RegisteredScopes) {

					if (scope.m_FrameEnabled == Time.frameCount) {
						// A bit of copy-paste from OnEnable(). Sad.
						switch (scope.OnEnableBehaviour) {
							case OnEnablePolicy.Focus:
								// Skip activation if this frame another scope was activated with higher priority.
								if (!m_PlayerSet.RegisteredScopes.Any(s => s.m_FrameEnabled == m_FrameEnabled && s.OnEnableBehaviour == OnEnablePolicy.FocusWithFramePriority)) {
									SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, CollectScopes(scope));
									return;
								}

								fallbackFrameScope = fallbackFrameScope ?? scope;
								break;

							case OnEnablePolicy.FocusWithFramePriority:
								SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, CollectScopes(scope));
								return;

							case OnEnablePolicy.FocusIfCurrentIsLowerDepth:
								if (m_ScopeDepth < scope.m_ScopeDepth) {
									SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, CollectScopes(scope));
									return;
								}

								fallbackFrameScope = fallbackFrameScope ?? scope;
								break;

							case OnEnablePolicy.FocusIfCurrentIsLowerOrEqualDepth:
								if (m_ScopeDepth <= scope.m_ScopeDepth) {
									SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, CollectScopes(scope));
									return;
								}

								fallbackFrameScope = fallbackFrameScope ?? scope;
								break;

							case OnEnablePolicy.DontFocus:
								// Enabled this frame, but didn't want to focus, so don't do it.
								break;

							default:
								throw new NotSupportedException(scope.OnEnableBehaviour.ToString());
						}
					}
				}

				// Focus on enabled scope this frame, even if it doesn't match the OnEnable criteria. Makes sense?
				if (fallbackFrameScope) {
					SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, CollectScopes(fallbackFrameScope));
				}

				UIScope nextScope = null;
				UIScope[] nextScopes;

				switch (OnDisableBehaviour) {
					case OnDisablePolicy.FocusParentScope:
						Transform scopeTransform = transform.parent;
						while(scopeTransform) {
							UIScope scope = scopeTransform.GetComponent<UIScope>();
							if (scope && scope.isActiveAndEnabled && scope.AutomaticFocus) {
								nextScope = scope;
								break;
							}

							scopeTransform = scopeTransform.parent;
						}
						break;

					case OnDisablePolicy.FocusPreviousScope:
						nextScope = m_LastFocusedScope;
						if (nextScope == null)
							break;

						var visitedScopes = new HashSet<UIScope>() { nextScope };

						while(nextScope != null && !nextScope.isActiveAndEnabled) {
							nextScope = visitedScopes.Contains(nextScope.m_LastFocusedScope)
								? null
								: nextScope.m_LastFocusedScope
								;
						}
						break;

					case OnDisablePolicy.FocusScopeWithHighestDepth:
						nextScope = m_PlayerSet.RegisteredScopes.FirstOrDefault();
						foreach(UIScope scope in m_PlayerSet.RegisteredScopes) {
							if (nextScope.m_ScopeDepth < scope.m_ScopeDepth && scope.isActiveAndEnabled) {
								nextScope = scope;
							}
						}
						break;

					case OnDisablePolicy.FocusFirstEnabledScopeFromList:
						nextScope = OnDisableScopes.FirstOrDefault(s => s.isActiveAndEnabled);
						break;

					case OnDisablePolicy.EmptyFocus:
						nextScope = null;
						break;

					default:
						throw new NotSupportedException(OnDisableBehaviour.ToString());
				}

				nextScopes = nextScope
					? CollectScopes(nextScope)
					: Array.Empty<UIScope>()
					;

				SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
			}
		}

		/// <summary>
		/// Deactivate the current active scopes and reactivate them back, forcing full reinitialization.
		/// NOTE: This will not rescan for new ScopeElements.
		/// </summary>
		public static void RefocusActiveScopes(PlayerContextUIRootObject playerUI)
		{
			s_PlayerSets.TryGetValue(playerUI, out PlayerScopeSet playerSet);

			if (playerSet == null || playerSet.ActiveScopes.Length == 0)
				return;

			var lastActive = playerSet.ActiveScopes.Last();

			if (playerSet.ChangingActiveScopes) {
				playerSet.PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(lastActive, true));
				return;
			}

			// Force full re-initialization of all the scopes.
			SwitchActiveScopes(playerSet, ref playerSet.ActiveScopes, new UIScope[0]);
			lastActive.Focus();
		}

		/// <summary>
		/// Deactivate the current active scopes and reactivate them back, forcing full reinitialization.
		/// NOTE: This will not rescan for new ScopeElements.
		/// </summary>
		public static void RefocusActiveScopes()
		{
			foreach(PlayerContextUIRootObject playerUI in s_PlayerSets.Keys) {
				RefocusActiveScopes(playerUI);
			}
		}

		/// <summary>
		/// Force selected scope to be active, instead of the last enabled.
		/// </summary>
		[ContextMenu("Focus scope")]
		public void Focus()
		{
			if (!m_HasInitialized) {
				Debug.LogWarning($"[Input] Couldn't focus on {name} as it isn't initialized.", this);
				return;
			}

			if (!AutomaticFocus) {
				throw new InvalidOperationException($"Trying to focus scope {name} that has set {nameof(AutomaticFocus)} to false. Just enable the scope and it will be active.");
			}

			if (m_PlayerSet.ChangingActiveScopes) {
				m_PlayerSet.PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(this, true));
				return;
			}

			// That would be weird.
			if (!gameObject.activeInHierarchy) {
				Debug.LogWarning($"[Input] Trying to force activate UIScope {name}, but it is not active in the hierarchy. Abort!", this);
				return;
			}

			UIScope[] nextScopes = CollectScopes(this);

			SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, nextScopes);
		}

		/// <summary>
		/// Is this scope active due to one of its children being focused, or being the focused one.
		/// </summary>
		public bool IsActive => m_PlayerSet != null && m_PlayerSet.ActiveScopes.Contains(this);

		/// <summary>
		/// Is this scope focused.
		/// </summary>
		public bool IsFocused => m_PlayerSet != null && m_PlayerSet.ActiveScopes.LastOrDefault() == this;

		/// <summary>
		/// Call this if you changed your UI hierarchy and expect added or removed scope elements.
		/// </summary>
		[ContextMenu("Rescan for owned scope elements")]
		public void ScanForOwnedScopeElements()
		{
			if (!m_HasInitialized) {
				Debug.LogWarning($"[Input] Couldn't scan owned scope elements for {name} as it isn't initialized.", this);
				return;
			}

			m_ScopeElements.Clear();
			m_DirectChildScopes.Clear();
			ScanForOwnedScopeElements(this, transform, m_ScopeElements, m_DirectChildScopes);

			ReapplyOwnedScopeElements();
		}

		/// <summary>
		/// This will replace the controlled scopeElements, no questions asked. Make sure passed elements are not owned by another scope.
		/// USE WITH CAUTION!
		/// </summary>
		public void ReplaceOwnedScopeElements(IEnumerable<IScopeElement> scopeElements)
		{
			if (!m_HasInitialized) {
				Debug.LogWarning($"[Input] Couldn't scan owned scope elements for {name} as it isn't initialized.", this);
				return;
			}

			m_ScopeElements.Clear();
			m_ScopeElements.AddRange(scopeElements);

			ReapplyOwnedScopeElements();
		}

		/// <summary>
		/// This will try to append scopeElements to the already controlled ones, if they are not already controlled, no questions asked. Make sure passed elements are not owned by another scope.
		/// USE WITH CAUTION!
		/// </summary>
		public void TryAppendOwnedScopeElements(IEnumerable<IScopeElement> scopeElements)
		{
			if (!m_HasInitialized) {
				Debug.LogWarning($"[Input] Couldn't scan owned scope elements for {name} as it isn't initialized.", this);
				return;
			}

			bool changed = false;
			foreach(IScopeElement scopeElement in scopeElements) {
				if (!m_ScopeElements.Contains(scopeElement)) {
					changed = true;
					m_ScopeElements.Add(scopeElement);
				}
			}

			if (changed) {
				ReapplyOwnedScopeElements();
			}
		}

		/// <summary>
		/// This will try to append scopeElement to the already controlled ones, if it is not already controlled, no questions asked. Make sure passed elements are not owned by another scope.
		/// USE WITH CAUTION!
		/// </summary>
		public void TryAppendOwnedScopeElements(IScopeElement scopeElement)
		{
			if (!m_HasInitialized) {
				Debug.LogWarning($"[Input] Couldn't scan owned scope elements for {name} as it isn't initialized.", this);
				return;
			}

			bool changed = false;
			if (!m_ScopeElements.Contains(scopeElement)) {
				changed = true;
				m_ScopeElements.Add(scopeElement);
			}

			if (changed) {
				ReapplyOwnedScopeElements();
			}
		}

		/// <summary>
		/// Remove destroyed (as in Unity destroyed) or non-child scope elements controlled by this scope.
		/// </summary>
		public void ClearNonOwnedChildScopeElements()
		{
			m_ScopeElements.RemoveAll(e => e is Component component && (component == null || !component.transform.IsChildOf(transform)));
		}

		public bool Owns(IScopeElement scopeElement)
		{
			return m_ScopeElements.Contains(scopeElement);
		}

		// Call this after list of scope elements has changed.
		private void ReapplyOwnedScopeElements()
		{
			if (Array.IndexOf(m_PlayerSet.ActiveScopes, this) != -1) {
				var lastActive = m_PlayerSet.ActiveScopes.Last();

				if (m_PlayerSet.ChangingActiveScopes) {
					m_PlayerSet.PendingScopeChanges.Enqueue(new KeyValuePair<UIScope, bool>(lastActive, true));
					return;
				}

				// Force full re-initialization of all the scopes including this one.
				SwitchActiveScopes(m_PlayerSet, ref m_PlayerSet.ActiveScopes, new UIScope[0]);
				lastActive.Focus();

			} else {

				if (AutomaticFocus) {
					foreach (IScopeElement scopeElement in m_ScopeElements) {
						scopeElement.enabled = false;
					}
				} else {
					// Make sure any new elements were re-initialized properly.
					SetScopeState(false);

					SetScopeState(true);
				}
			}
		}

		internal static void ScanForOwnedScopeElements(UIScope parentScope, Transform transform, List<IScopeElement> scopeElements, List<UIScope> directChildScopes)
		{
			var scope = transform.GetComponent<UIScope>();
			// Another scope begins, it will handle its own child hotkey elements.
			if (scope && parentScope != scope) {
				directChildScopes.Add(scope);
				return;
			}

			scopeElements.AddRange(transform.GetComponents<IScopeElement>());

			foreach(Transform child in transform) {
				ScanForOwnedScopeElements(parentScope, child, scopeElements, directChildScopes);
			}
		}

		protected static UIScope[] CollectScopes(Component target)
		{
			return target
				.GetComponentsInParent<UIScope>(true)	// Collect all so they get deactivated properly later on.
				.Reverse()
				.Where(s => s.enabled && s.AutomaticFocus)
				.ToArray();
		}

		protected static void SwitchActiveScopes(PlayerScopeSet playerSet, ref UIScope[] prevScopes, UIScope[] nextScopes)
		{
			// Find if there is a root scope and trim the parents.
			// Do it now, at the last moment, instead of CollectScopes(), so correct depth can be set.
			int rootIndex = Array.FindLastIndex(nextScopes, s => s.IsRoot);
			if (rootIndex > 0) {
				nextScopes = new ArraySegment<UIScope>(nextScopes, rootIndex, nextScopes.Length - rootIndex).ToArray();
			}

			// Switching scopes may trigger user code that may switch scopes indirectly, while already doing so.
			// Any such change will be pushed to a queue and applied later on.
			// TODO: This was never really tested.
			playerSet.ChangingActiveScopes = true;

			try {
				foreach (UIScope scope in prevScopes) {
					if (!nextScopes.Contains(scope)) {
						scope.Deactivating?.Invoke();
						ScopeDeactivating?.Invoke(scope);
					}
				}

				UIScope prevFocusedScope = prevScopes.LastOrDefault();
				if (prevFocusedScope) {
					prevFocusedScope.Unfocusing?.Invoke();
					ScopeUnfocusing?.Invoke(prevFocusedScope);
				}

				// Reversed order, just in case.
				foreach (UIScope scope in prevScopes.Reverse()) {
					if (!nextScopes.Contains(scope)) {
						scope.SetScopeState(false);
					}
				}

				foreach (UIScope scope in nextScopes) {
					if (!prevScopes.Contains(scope)) {
						scope.SetScopeState(true);
						scope.Activated?.Invoke();
						ScopeActivated?.Invoke(scope);
					}
				}

				UIScope nextFocusedScope = nextScopes.LastOrDefault();
				if (nextFocusedScope) {
					nextFocusedScope.Focused?.Invoke();
					ScopeFocused?.Invoke(nextFocusedScope);
				}
			}

			finally {
				prevScopes = nextScopes;
				playerSet.ChangingActiveScopes = false;
			}

			while(playerSet.PendingScopeChanges.Count > 0) {
				var scopeChange = playerSet.PendingScopeChanges.Dequeue();

				if (scopeChange.Value) {
					scopeChange.Key.OnEnable();
				} else {
					scopeChange.Key.OnDisable();
				}
			}
		}

		protected virtual void SetScopeState(bool active)
		{
			// If this scope isn't still initialized, do it now, or no elements will be enabled.
			// This happens when child scope tries to activate the parent scope for the first time, while the parent was still inactive.
			if (!m_HasInitialized) {
				return;
			}

			PreProcessInput(active);

			foreach(IScopeElement scopeElement in m_ScopeElements) {

				// If scope is already in this state, it is probably the initial run.
				// Make sure it toggles so it initializes correctly (e.g. hide objects on disable, instead of leaving them out there).
				if (scopeElement.enabled == active) {
					scopeElement.enabled = !active;
				}

				scopeElement.enabled = active;
			}

			PostProcessInput(active);
		}

		protected void PreProcessInput(bool active)
		{
#if USE_INPUT_SYSTEM
			var context = m_PlayerContext.InputContext;

			if (context == null) {
				Debug.LogWarning($"[Input] {nameof(UIScope)} {name} can't be used if Unity Input System is not provided.", this);
				return;
			}

			// Pushing input on stack will reset the actions anyway.
			if (ResetAllActionsOnEnable && active && !PushInputStack) {

				// Resets all enabled actions. This will interrupt their progress and any gesture, drag, sequence will be canceled.
				// Useful on changing states or scopes, so gestures, drags, sequences don't leak in.
				foreach (var action in context.GetAllActions()) {
					if (action.enabled) {
						action.Reset();
					}
				}
			}

			if (active) {

				if (PushInputStack) {
					context.PushActionsState(this);

					if (IncludeUIActions) {
						foreach (var action in context.GetUIActions()) {
							action.Enable();
						}
					}
				}

			}
#endif
		}

		protected void PostProcessInput(bool active)
		{
#if USE_INPUT_SYSTEM
			var context = m_PlayerContext.InputContext;

			if (context == null)
				return;

			if (!active) {
				if (PushInputStack) {
					context.PopActionsState(this);
				}
			}
#endif
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(UIScope), true)]
	[UnityEditor.CanEditMultipleObjects]
	internal class UIScopeEditor : UnityEditor.Editor
	{
		private static bool m_InfoFoldOut = false;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			UnityEditor.EditorGUI.BeginDisabledGroup(true);
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			UnityEditor.EditorGUI.EndDisabledGroup();

			var uiScope = (UIScope)target;

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.IsRoot)));
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.AutomaticFocus)));

			if (uiScope.AutomaticFocus) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.OnEnableBehaviour)));
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.OnDisableBehaviour)));

				if (uiScope.OnDisableBehaviour == UIScope.OnDisablePolicy.FocusFirstEnabledScopeFromList) {
					UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.OnDisableScopes)));
				}
			}

#if USE_INPUT_SYSTEM
			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.ResetAllActionsOnEnable)));

			UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.PushInputStack)));
			if (uiScope.PushInputStack) {
				UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UIScope.IncludeUIActions)));
			}
#endif

			serializedObject.ApplyModifiedProperties();


			var scopeElements = new List<IScopeElement>();
			var directChildScopes = new List<UIScope>();
			UIScope.ScanForOwnedScopeElements(uiScope, uiScope.transform, scopeElements, directChildScopes);


			UnityEditor.EditorGUILayout.Space();


			UnityEditor.EditorGUILayout.BeginHorizontal();
			m_InfoFoldOut = UnityEditor.EditorGUILayout.BeginFoldoutHeaderGroup(m_InfoFoldOut, "Info");
			if (GUILayout.Button("Open Scopes Debugger", UnityEditor.EditorStyles.miniButton, GUILayout.ExpandWidth(false))) {
				UIScopesDebugger.Init();
			}
			UnityEditor.EditorGUILayout.EndHorizontal();

			if (m_InfoFoldOut) {

				UnityEditor.EditorGUILayout.BeginVertical(UnityEditor.EditorStyles.helpBox);

				string scopeState = "Inactive";
				Color scopeStateColor = Color.red;
				if (uiScope.IsFocused) {
					scopeState = "Focused";
					scopeStateColor = Color.green;
				} else if (uiScope.IsActive) {
					scopeState = "Active";
					scopeStateColor = Color.yellow;
				}


				var prevColor = GUI.color;

				UnityEditor.EditorGUILayout.BeginHorizontal();
				UnityEditor.EditorGUILayout.LabelField("Scope State:", UnityEditor.EditorStyles.boldLabel, GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth));
				GUI.color = scopeStateColor;
				UnityEditor.EditorGUILayout.LabelField(scopeState, UnityEditor.EditorStyles.boldLabel);
				GUI.color = prevColor;
				UnityEditor.EditorGUILayout.EndHorizontal();

				UnityEditor.EditorGUILayout.LabelField("Controlled Elements:", UnityEditor.EditorStyles.boldLabel);

				foreach (var element in scopeElements) {
					UnityEditor.EditorGUILayout.BeginHorizontal();
					UnityEditor.EditorGUILayout.ObjectField(element as UnityEngine.Object, typeof(IScopeElement), true);

#if USE_INPUT_SYSTEM
					if (element is IHotkeysWithInputActions hotkeyElement) {


						bool actionsActive = uiScope.enabled
												&& uiScope.gameObject.activeInHierarchy
												&& uiScope.m_PlayerContext?.InputContext != null
												&& hotkeyElement.GetUsedActions(uiScope.m_PlayerContext.InputContext).Any(a => a.enabled);

						string activeStr = actionsActive ? "Active" : "Inactive";
						GUI.color = actionsActive ? Color.green : Color.red;

						GUILayout.Label(new GUIContent(activeStr, "Are the hotkey input actions active or not?"), GUILayout.ExpandWidth(false));
						GUI.color = prevColor;
					}
#endif
					UnityEditor.EditorGUILayout.EndHorizontal();
				}

				UnityEditor.EditorGUILayout.EndVertical();

			}

			UnityEditor.EditorGUILayout.EndFoldoutHeaderGroup();

		}
	}
#endif
}