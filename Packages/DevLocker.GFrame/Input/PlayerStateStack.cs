using System;
using System.Collections;
using System.Collections.Generic;

namespace DevLocker.GFrame.Input
{
	/// <summary>
	/// Single player state. It can be different playing modes (walking, driving, swimming) or UI screens (Menu, Game, Options).
	/// To avoid coupling & singletons, get the needed references to other systems using the <see cref="PlayerStatesContext"/> references collection.
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public interface IPlayerState
	{
		void EnterState(PlayerStatesContext context);
		void ExitState();
	}

	public enum StackAction
	{
		ClearAndPush,
		Push,
		ReplaceTop,
	}


	/// <summary>
	/// Stack of player states. States can be pushed in / replaced / popped out of the stack.
	/// On switching states, EnterState() & ExitState() methods are invoked.
	/// To avoid coupling & singletons, states should get the needed references to other systems using the <see cref="PlayerStatesContext"/> references collection.
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public class PlayerStateStack
	{
		private class PendingStateArgs
		{
			public IPlayerState State;
			public StackAction StackAction;

			public PendingStateArgs(IPlayerState state, StackAction action)
			{
				State = state;
				StackAction = action;
			}
		}

		public IPlayerState CurrentState => IsEmpty ? null : m_StackedStates.Peek();
		public bool IsEmpty => m_StackedStates.Count == 0;

		public int StackedStatesCount => m_StackedStates.Count;

		/// <summary>
		/// Collection of static level references to be accessed by the states.
		/// References are provided by the level supervisor on initialization.
		/// </summary>
		public PlayerStatesContext Context { get; private set; }


		// Listen for state change.
		// NOTE: avoid using events with more complex logic as it will blow up in your face.
		//		 If you really need to do it, you can inherit this class and override the corresponding protected methods.
		public event Action ExitingState;
		public event Action ExitedState;
		public event Action EnteringState;
		public event Action EnteredState;

		public event Action StateChangesStarted;
		public event Action StateChangesEnded;

		private readonly Stack<IPlayerState> m_StackedStates = new Stack<IPlayerState>();

		// Used when state is changed inside another state change event.
		public bool ChangingStates { get; private set; } = false;
		private Queue<PendingStateArgs> m_PendingStateChanges = new Queue<PendingStateArgs>();

		/// <summary>
		/// Pass in a list of static context level references to be used by the states.
		/// </summary>
		public PlayerStateStack(params object[] context)
		{
			Context = new PlayerStatesContext(context);
		}


		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// </summary>
		public void PushState(IPlayerState state)
		{
			ChangeState(state, StackAction.Push);
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// </summary>
		public void SetState(IPlayerState state)
		{
			ChangeState(state, StackAction.ClearAndPush);
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// </summary>
		public void PopState()
		{
			PopStates(1);
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// </summary>
		public void PopStates(int count)
		{
			count = Math.Max(1, count);

			if (StackedStatesCount < count) {
				UnityEngine.Debug.LogError("Trying to pop states while there aren't any stacked ones.");
				return;
			}

			OnExitingState();
			CurrentState.ExitState();
			OnExitedState();

			for (int i = 0; i < count; ++i) {
				m_StackedStates.Pop();
			}

			OnEnteringState();
			CurrentState?.EnterState(Context);
			OnEnteredState();
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// </summary>
		public void ReenterCurrentState()
		{
			// Re-insert the top state to trigger changing events.
			ChangeState(CurrentState, StackAction.ReplaceTop);
		}

		/// <summary>
		/// Exits the state and leaves the stack empty.
		/// </summary>
		public void ClearStackAndState()
		{
			PopStates(StackedStatesCount);
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// </summary>
		public void ChangeState(IPlayerState state, StackAction stackAction)
		{
			// Sanity check.
			if (m_StackedStates.Count > 7 && stackAction == StackAction.Push) {
				UnityEngine.Debug.LogWarning($"You're stacking too many states down. Are you sure? Stacked state: {state}.");
			}

			if (ChangingStates) {
				m_PendingStateChanges.Enqueue(new PendingStateArgs(state, stackAction));

			} else {
				ChangingStates = true;
				StateChangesStarted?.Invoke();
			}

			if (CurrentState != null) {
				OnExitingState();
				CurrentState.ExitState();
				OnExitedState();
			}

			if (stackAction == StackAction.ClearAndPush) {
				m_StackedStates.Clear();
			}

			if (stackAction == StackAction.ReplaceTop && m_StackedStates.Count > 0) {
				m_StackedStates.Pop();
			}

			m_StackedStates.Push(state);

			OnEnteringState();
			CurrentState.EnterState(Context);
			OnEnteredState();

			ChangingStates = false;
			StateChangesEnded?.Invoke();

			// Execute the pending states...
			if (m_PendingStateChanges.Count > 0) {
				var stateArgs = m_PendingStateChanges.Dequeue();

				ChangeState(stateArgs.State, stateArgs.StackAction);
			}
		}


		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual void OnExitingState()
		{
			ExitingState?.Invoke();
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual void OnExitedState()
		{
			ExitedState?.Invoke();
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual void OnEnteringState()
		{
			EnteringState?.Invoke();
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual void OnEnteredState()
		{
			EnteredState?.Invoke();

			// In case the scopes were already active when the state kicked in and it pushed
			// a new state onto the InputActionsStack (resetting the previous actions).
			Input.UIScope.UIScope.RefocusActiveScopes();
		}
	}
}