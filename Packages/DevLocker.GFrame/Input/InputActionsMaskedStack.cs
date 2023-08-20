#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Keeps track of the input actions state (enable flag).
	/// Can push a mask for what actions are allowed to be enabled (others will be automatically disabled until mask is removed).
	/// For the mask to work, enabling and disabling actions must always be done via this class, not directly by InputAction.Enable().
	/// Masks are stacked and only the top one is active.
	/// </summary>
	public class InputActionsMaskedStack
	{
		private class StateSourceBind
		{
			public object Source;
			public HashSet<InputAction> MaskActions;
		}

		public struct InputActionConflictsReport
		{
			public List<KeyValuePair<InputAction, IReadOnlyCollection<object>>> Conflicts => m_Conflicts != null ? m_Conflicts : (m_Conflicts = new ());
			private List<KeyValuePair<InputAction, IReadOnlyCollection<object>>> m_Conflicts;

			/// <summary>
			/// Compare if two collections of conflicts are equal.
			/// </summary>
			public bool Equals(InputActionConflictsReport conflictsReport)
			{
				return conflictsReport.Conflicts.SequenceEqual(Conflicts);
			}
		}

		public IReadOnlyCollection<InputAction> Actions => m_Actions.Keys;

		private Dictionary<InputAction, HashSet<object>> m_Actions = new Dictionary<InputAction, HashSet<object>>();

		private List<StateSourceBind> m_MasksStack = new List<StateSourceBind>();

		private HashSet<InputAction> m_CurrentActionsMask => m_MasksStack.LastOrDefault()?.MaskActions;

		public InputActionsMaskedStack(IInputActionCollection2 actions)
		{
			foreach(InputAction action in actions) {
				m_Actions.Add(action, new HashSet<object>(2));
			}
		}

		/// <summary>
		/// Enable action if mask stack allows it.
		/// Remembers the enable state in case the mask gets removed later on.
		///
		/// Enable requests are ref-counted by the source objects. No source object requests, action will be disabled.
		/// </summary>
		public void Enable(object source, InputAction action)
		{
			if (source == null || action == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Enabling by source of type InputAction is not allowed.");

			if (!m_Actions.TryGetValue(action, out HashSet<object> enableSources))
				throw new ArgumentException($"Input action \"{action}\" is not part of the tracked actions.");

			enableSources.Add(source);

			if (m_CurrentActionsMask?.Contains(action) ?? true) {
				action.Enable();
			}
		}

		/// <summary>
		/// Disable action.
		/// Remembers the disable state for later updates.
		///
		/// Enable requests are ref-counted by the source objects. No source object requests, action will be disabled.
		/// </summary>
		public void Disable(object source, InputAction action)
		{
			if (source == null || action == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Disabling by source of type InputAction is not allowed.");

			if (!m_Actions.TryGetValue(action, out HashSet<object> enableSources))
				throw new ArgumentException($"Input action \"{action}\" is not part of the tracked actions.");

			enableSources.Remove(source);

			if (enableSources.Count == 0) {
				action.Disable();
			}
		}

		/// <summary>
		/// Disable all input actions enabled by the provided source object.
		///
		/// Enable requests are ref-counted by the source objects. No source object requests, action will be disabled.
		/// </summary>
		public void Disable(object source)
		{
			if (source == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Disabling by of type source InputAction is not allowed.");

			foreach (var pair in m_Actions) {
				if (pair.Value.Remove(source) && pair.Value.Count == 0) {
					pair.Key.Disable();
				}
			}
		}

		/// <summary>
		/// Returns all input actions enabled by specified source.
		/// </summary>
		public IEnumerable<InputAction> GetInputActionsEnabledBy(object source)
		{
			if (source == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Disabling by of type source InputAction is not allowed.");

			foreach (var pair in m_Actions) {
				if (pair.Value.Contains(source)) {
					yield return pair.Key;
				}
			}
		}

		/// <summary>
		/// Push actions mask filtering in actions allowed to be enabled.
		/// If mask is added or set to the top of the stack it will be applied immediately disabling any actions not included.
		/// </summary>
		public void PushOrSetActionsMask(object source, IEnumerable<InputAction> actionsMask, bool setBackToTop = false)
		{
			if (source == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Disabling by of type source InputAction is not allowed.");

			foreach (StateSourceBind pair in m_MasksStack) {
				if (pair.Source == source) {
					pair.MaskActions = actionsMask.ToHashSet();

					if (setBackToTop) {
						// Should work, as we stop iterating right away.
						m_MasksStack.Remove(pair);
						break;
					}

					if (pair == m_MasksStack.Last()) {
						ForceRefreshInputActionStates();
					}
					return;
				}
			}

			m_MasksStack.Add(new StateSourceBind {
				Source = source,
				MaskActions = actionsMask.ToHashSet(),
			});
			ForceRefreshInputActionStates();
		}

		/// <summary>
		/// Remove actions mask by the source it pushed it.
		/// Removing it will restore the actions state according to the next mask in the stack or their original tracked state.
		/// </summary>
		public void PopActionsMask(object source)
		{
			if (source == null)
				throw new ArgumentNullException();

			if (source is InputAction)
				throw new ArgumentException("Disabling by of type source InputAction is not allowed.");

			for (int i = 0; i < m_MasksStack.Count; ++i) {
				StateSourceBind pair = m_MasksStack[i];

				if (pair.Source == source) {
					bool wasActive = i == m_MasksStack.Count - 1;

					m_MasksStack.RemoveAt(i);

					if (wasActive) {
						ForceRefreshInputActionStates();
					}
					return;
				}
			}
		}

		/// <summary>
		/// Should action be enabled according to the tracked state or currently active actions mask.
		/// </summary>
		public bool ShouldActionBeEnabled(InputAction action)
		{
			if (m_MasksStack.Count > 0) {
				if (m_CurrentActionsMask.Contains(action)) {
					return m_Actions[action].Count > 0;
				} else {
					return false;
				}
			} else {
				return m_Actions[action].Count > 0;
			}
		}

		/// <summary>
		/// Returns a list of conflicting InputActions and the sources that enabled them.
		/// </summary>
		public InputActionConflictsReport GetConflictingActionRequests(IEnumerable<InputAction> uiActions)
		{
			var conflictsReport = new InputActionConflictsReport();

			foreach(var pair in m_Actions) {

				if (pair.Value.Count <= 1)
					continue;

				// If there are two users, one from the player state, another from the mask (i.e. modal dialog), it is ok to have conflicts.
				// At least for the UI actions.
				if (pair.Value.Count == 2 && m_MasksStack.Count > 0 && m_CurrentActionsMask.Contains(pair.Key) && uiActions.Contains(pair.Key))
					continue;

				conflictsReport.Conflicts.Add(KeyValuePair.Create<InputAction, IReadOnlyCollection<object>>(pair.Key, pair.Value));
			}

			return conflictsReport;
		}

		/// <summary>
		/// Cancel all enable requests and disable all input actions.
		/// </summary>
		public void ForceClearAllEnableRequests(IEnumerable<InputAction> exclude = null)
		{
			foreach(var pair in m_Actions) {
				if (exclude != null && exclude.Contains(pair.Key))
					continue;

				pair.Key.Disable();
				pair.Value.Clear();
			}
		}

		/// <summary>
		/// Reapply the input actions state according to the known tracked one or the currently active mask.
		/// </summary>
		public void ForceRefreshInputActionStates()
		{
			foreach(var pair in m_Actions) {
				InputAction action = pair.Key;
				HashSet<object> enableSources = pair.Value;

				if (m_MasksStack.Count > 0) {

					if (m_CurrentActionsMask.Contains(action)) {

						if (enableSources.Count > 0) {
							action.Enable();
						} else {
							action.Disable();
						}

					} else {
						action.Disable();
					}

				} else {

					if (enableSources.Count > 0) {
						action.Enable();
					} else {
						action.Disable();
					}
				}
			}
		}
	}
}
#endif