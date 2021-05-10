using System;
using System.Collections;
using System.Collections.Generic;

namespace DevLocker.GameFrame
{
	/// <summary>
	/// Single level state. It can be different playing modes (walking, driving, swimming) or UI screens (Menu, Game, Options).
	/// To avoid coupling & singletons, get the needed references to other systems using the contextReferences collection.
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public interface ILevelState
	{
		IEnumerator EnterState(LevelStateContextReferences contextReferences);
		IEnumerator ExitState();
	}

	public enum StackAction
	{
		ClearAndPush,
		Push,
		ReplaceTop,
	}


	/// <summary>
	/// Stack of level states. States can be pushed in / replaced / popped out of the stack.
	/// On switching states, EnterState() & ExitState() methods are invoked.
	/// To avoid coupling & singletons, states should get the needed references to other systems using the ContextReferences collection.
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public class LevelStateStack
	{
		private class PendingStateArgs
		{
			public ILevelState State;
			public StackAction StackAction;

			public PendingStateArgs(ILevelState state, StackAction action)
			{
				State = state;
				StackAction = action;
			}
		}

		public ILevelState CurrentState => IsEmpty ? null : m_StackedStates.Peek();
		public bool IsEmpty => m_StackedStates.Count == 0;

		public int StackedStatesCount => m_StackedStates.Count;

		/// <summary>
		/// Collection of static level references to be accessed by the states.
		/// References are provided by the level supervisor on initialization.
		/// </summary>
		public LevelStateContextReferences ContextReferences { get; private set; }

		private readonly Stack<ILevelState> m_StackedStates = new Stack<ILevelState>();

		// Used when state is changed inside another state change event.
		private bool m_ChangingStates = false;
		private Queue<PendingStateArgs> m_PendingStateChanges = new Queue<PendingStateArgs>();

		/// <summary>
		/// Pass in a list of static context level references to be used by the states.
		/// </summary>
		public LevelStateStack(params object[] contextReferences)
		{
			ContextReferences = new LevelStateContextReferences(contextReferences);
		}

		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// </summary>
		public IEnumerator PushStateCrt(ILevelState state)
		{
			yield return ChangeStateCrt(state, StackAction.Push);
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// </summary>
		public IEnumerator SetStateCrt(ILevelState state)
		{
			yield return ChangeStateCrt(state, StackAction.ClearAndPush);
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// </summary>
		public IEnumerator PopStateCrt()
		{
			yield return PopStatesCrt(1);
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// </summary>
		public IEnumerator PopStatesCrt(int count)
		{
			count = Math.Max(1, count);

			if (StackedStatesCount < count) {
				UnityEngine.Debug.LogError("Trying to pop states while there aren't any stacked ones.");
				yield break;
			}

			yield return CurrentState.ExitState();

			for (int i = 0; i < count; ++i) {
				m_StackedStates.Pop();
			}

			yield return CurrentState?.EnterState(ContextReferences);
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// </summary>
		public IEnumerator ReenterCurrentStateCrt()
		{
			// Re-insert the top state to trigger changing events.
			yield return ChangeStateCrt(CurrentState, StackAction.ReplaceTop);
		}

		/// <summary>
		/// Exits the state and leaves the stack empty.
		/// </summary>
		public IEnumerator ClearStackAndStateCrt()
		{
			yield return PopStatesCrt(StackedStatesCount);
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// </summary>
		public IEnumerator ChangeStateCrt(ILevelState state, StackAction stackAction)
		{
			// Sanity check.
			if (m_StackedStates.Count > 7 && stackAction == StackAction.Push) {
				UnityEngine.Debug.LogWarning($"You're stacking too many states down. Are you sure? Stacked state: {state}.");
			}

			if (m_ChangingStates) {
				m_PendingStateChanges.Enqueue(new PendingStateArgs(state, stackAction));

				// Wait till all the state switching has finished. That means that you may end up in a state that is not the one you requested.
				while(m_PendingStateChanges.Count > 0) {
					yield return null;
				}

			} else {
				m_ChangingStates = true;
			}

			yield return CurrentState?.ExitState();

			if (stackAction == StackAction.ClearAndPush) {
				m_StackedStates.Clear();
			}

			if (stackAction == StackAction.ReplaceTop && m_StackedStates.Count > 0) {
				m_StackedStates.Pop();
			}

			m_StackedStates.Push(state);

			yield return CurrentState.EnterState(ContextReferences);

			m_ChangingStates = false;

			// Execute the pending states...
			if (m_PendingStateChanges.Count > 0) {
				var stateArgs = m_PendingStateChanges.Dequeue();

				yield return ChangeStateCrt(stateArgs.State, stateArgs.StackAction);
			}
		}
	}
}