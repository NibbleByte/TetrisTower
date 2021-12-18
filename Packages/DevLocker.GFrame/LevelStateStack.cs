using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Single level state. It can be different playing modes (walking, driving, swimming) or UI screens (Menu, Game, Options).
	/// To avoid coupling & singletons, get the needed references to other systems using the contextReferences collection.
	/// References are provided by the level supervisor on initialization.
	/// </summary>
	public interface ILevelState
	{
#if GFRAME_ASYNC
		Task EnterStateAsync(LevelStateContextReferences contextReferences);
		Task ExitStateAsync();
#else
		IEnumerator EnterState(LevelStateContextReferences contextReferences);
		IEnumerator ExitState();
#endif
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


		// Listen for state change.
		// NOTE: avoid using events with more complex logic as it will blow up in your face.
		//		 If you really need to do it, you can inherit this class and override the corresponding protected methods.
		public event Action ExitingState;
		public event Action ExitedState;
		public event Action EnteringState;
		public event Action EnteredState;

		private readonly Stack<ILevelState> m_StackedStates = new Stack<ILevelState>();

		// Used when state is changed inside another state change event.
		public bool ChangingStates { get; private set; } = false;
		private Queue<PendingStateArgs> m_PendingStateChanges = new Queue<PendingStateArgs>();

		/// <summary>
		/// Pass in a list of static context level references to be used by the states.
		/// </summary>
		public LevelStateStack(params object[] contextReferences)
		{
			ContextReferences = new LevelStateContextReferences(contextReferences);
		}

#if GFRAME_ASYNC

		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// </summary>
		public async Task PushStateAsync(ILevelState state)
		{
			await ChangeStateAsync(state, StackAction.Push);
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// </summary>
		public async Task SetStateAsync(ILevelState state)
		{
			await ChangeStateAsync(state, StackAction.ClearAndPush);
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// </summary>
		public async Task PopStateAsync()
		{
			await PopStatesAsync(1);
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// </summary>
		public async Task PopStatesAsync(int count)
		{
			count = Math.Max(1, count);

			if (StackedStatesCount < count) {
				UnityEngine.Debug.LogError("Trying to pop states while there aren't any stacked ones.");
				return;
			}

			await ExitingStateAsync();
			await CurrentState.ExitStateAsync();
			await ExitedStateAsync();

			for (int i = 0; i < count; ++i) {
				m_StackedStates.Pop();
			}

			await EnteringStateAsync();
			await (CurrentState?.EnterStateAsync(ContextReferences) ?? Task.CompletedTask);
			await EnteredStateAsync();
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// </summary>
		public async Task ReenterCurrentStateAsync()
		{
			// Re-insert the top state to trigger changing events.
			await ChangeStateAsync(CurrentState, StackAction.ReplaceTop);
		}

		/// <summary>
		/// Exits the state and leaves the stack empty.
		/// </summary>
		public async Task ClearStackAndStateAsync()
		{
			await PopStatesAsync(StackedStatesCount);
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// </summary>
		public async Task ChangeStateAsync(ILevelState state, StackAction stackAction)
		{
			// Sanity check.
			if (m_StackedStates.Count > 7 && stackAction == StackAction.Push) {
				UnityEngine.Debug.LogWarning($"You're stacking too many states down. Are you sure? Stacked state: {state}.");
			}

			if (ChangingStates) {
				m_PendingStateChanges.Enqueue(new PendingStateArgs(state, stackAction));

				// Wait till all the state switching has finished. That means that you may end up in a state that is not the one you requested.
				while (m_PendingStateChanges.Count > 0) {
					await Task.Yield();
				}

			} else {
				ChangingStates = true;
			}

			if (CurrentState != null) {
				await ExitingStateAsync();
				await CurrentState.ExitStateAsync();
				await ExitedStateAsync();
			}

			if (stackAction == StackAction.ClearAndPush) {
				m_StackedStates.Clear();
			}

			if (stackAction == StackAction.ReplaceTop && m_StackedStates.Count > 0) {
				m_StackedStates.Pop();
			}

			m_StackedStates.Push(state);

			await EnteringStateAsync();
			await CurrentState.EnterStateAsync(ContextReferences);
			await EnteredStateAsync();

			ChangingStates = false;

			// Execute the pending states...
			if (m_PendingStateChanges.Count > 0) {
				var stateArgs = m_PendingStateChanges.Dequeue();

				await ChangeStateAsync(stateArgs.State, stateArgs.StackAction);
			}
		}


		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task ExitingStateAsync()
		{
			ExitingState?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task ExitedStateAsync()
		{
			ExitedState?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task EnteringStateAsync()
		{
			EnteringState?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task EnteredStateAsync()
		{
			EnteredState?.Invoke();

			// In case the scopes were already active when the state kicked in and it pushed
			// a new state onto the InputActionsStack (resetting the previous actions).
			Input.UIScope.UIScope.RefocusActiveScopes();

			return Task.CompletedTask;
		}

#else

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

			yield return ExitingStateCrt();
			yield return CurrentState.ExitState();
			yield return ExitedStateCrt();

			for (int i = 0; i < count; ++i) {
				m_StackedStates.Pop();
			}

			yield return EnteringStateCrt();
			yield return CurrentState?.EnterState(ContextReferences);
			yield return EnteredStateCrt();
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

			if (ChangingStates) {
				m_PendingStateChanges.Enqueue(new PendingStateArgs(state, stackAction));

				// Wait till all the state switching has finished. That means that you may end up in a state that is not the one you requested.
				while(m_PendingStateChanges.Count > 0) {
					yield return null;
				}

			} else {
				ChangingStates = true;
			}

			if (CurrentState != null) {
				yield return ExitingStateCrt();
				yield return CurrentState.ExitState();
				yield return ExitedStateCrt();
			}

			if (stackAction == StackAction.ClearAndPush) {
				m_StackedStates.Clear();
			}

			if (stackAction == StackAction.ReplaceTop && m_StackedStates.Count > 0) {
				m_StackedStates.Pop();
			}

			m_StackedStates.Push(state);

			yield return EnteringStateCrt();
			yield return CurrentState.EnterState(ContextReferences);
			yield return EnteredStateCrt();

			ChangingStates = false;

			// Execute the pending states...
			if (m_PendingStateChanges.Count > 0) {
				var stateArgs = m_PendingStateChanges.Dequeue();

				yield return ChangeStateCrt(stateArgs.State, stateArgs.StackAction);
			}
		}


		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator ExitingStateCrt()
		{
			ExitingState?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator ExitedStateCrt()
		{
			ExitedState?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator EnteringStateCrt()
		{
			EnteringState?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator EnteredStateCrt()
		{
			EnteredState?.Invoke();

			// In case the scopes were already active when the state kicked in and it pushed
			// a new state onto the InputActionsStack (resetting the previous actions).
			Input.UIScope.UIScope.RefocusActiveScopes();

			yield break;
		}
#endif

	}
}