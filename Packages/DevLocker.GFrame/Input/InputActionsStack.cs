#if USE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Stack that keeps all InputActions' enabled flags in a single entry.
	/// This is a named stack, i.e. it keeps track of the source who pushed the new entry.
	/// When that source requests a pop out, its entry will be removed, not the top one.
	/// If that happens to be the top entry, the next entry input actions will be applied.
	/// The top entry doesn't store the current enabled flags (it's null), so you can enable / disable InputActions as much as you want.
	/// If another source requests a push, the current flags state will be recorded in the entry at the top,
	/// then a push will be initiated, creating the new top entry.
	///
	/// PRO TIP: Prefer sticking the <see cref="InputActionsStack" /> in the IInputActionCollection itself using the partial feature.
	///			 Example:
	///
	///			 public partial class PlayerControls : IInputActionCollection, IDisposable
	///			 {
	///			 	public InputActionsStack InputStack { get; private set; }
	///
	///			 	public void InitStack()
	///			 	{
	///			 		InputStack = new InputActionsStack(this);
	///			 		Disable();
	///			 	}
	///			 }
	/// </summary>
	public class InputActionsStack
	{
		private class StateSourceBind
		{
			public object Source;
			public Dictionary<Guid, bool> ActionsState;
		}

		private IInputActionCollection2 m_Actions;

		private List<StateSourceBind> m_Stack = new List<StateSourceBind>();

		public InputActionsStack(IInputActionCollection2 actions)
		{
			m_Actions = actions;

			m_Stack.Add(new StateSourceBind());
		}

		public void PushActionsState(object source, bool resetActions = true)
		{
			if (source == null) {
				throw new ArgumentNullException();
			}

			var actionsState = new Dictionary<Guid, bool>();
			foreach(var action in m_Actions) {
				actionsState.Add(action.id, action.enabled);
			}

			m_Stack[m_Stack.Count - 1].ActionsState = actionsState;
			m_Stack.Add(new StateSourceBind() { Source = source });

			if (resetActions) {
				m_Actions.Disable();
			}
		}

		public bool PopActionsState(object source)
		{
			if (source == null) {
				throw new ArgumentNullException();
			}

			for (int i = 0; i < m_Stack.Count; ++i) {
				var bind = m_Stack[i];

				if (source == bind.Source) {

					if (i == m_Stack.Count - 1) {
						RestoreState(m_Stack[i - 1]);
					}

					m_Stack.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		private void RestoreState(StateSourceBind bind)
		{
			foreach (var statePair in bind.ActionsState) {
				var action = m_Actions.FindAction(statePair.Key.ToString(), true);
				if (statePair.Value) {

					if (action.enabled) {
						action.Reset();
					} else {
						action.Enable();
					}

				} else {
					action.Disable();
				}
			}
		}
	}

}
#endif