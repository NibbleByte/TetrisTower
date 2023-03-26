using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Contains the currently active level supervisor and can switch it to another.
	/// </summary>
	public class LevelsManager : MonoBehaviour
	{
		/// <summary>
		/// Assign this if you want to show / hide loading screen between your levels (e.g. fade out effects).
		/// You may assign this multiple times based on your needs and next level to load.
		/// If null, it will be skipped.
		/// </summary>
		public ILevelLoadingScreen LevelLoadingScreen;

		[Tooltip("Should level loading screen show before exiting the level states or after.")]
		public bool ShowLoadingScreenBeforeLevelStates = false;

		public ILevelSupervisor LevelSupervisor { get; private set; }

		// Listen for supervisor change.
		// NOTE: avoid using events with more complex logic as it will blow up in your face.
		//		 If you really need to do it, you can inherit this LevelsManager and override the corresponding protected methods.
		public event Action UnloadingSupervisor;
		public event Action UnloadedSupervisor;
		public event Action LoadingSupervisor;
		public event Action LoadedSupervisor;

		protected virtual void Update()
		{
			if (LevelSupervisor is IUpdateListener updateSupervisor) {
				updateSupervisor.Update();
			}

			foreach(PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

				if (playerContext.StatesStack?.CurrentState is IUpdateListener updateState && !playerContext.StatesStack.ChangingStates) {
					updateState.Update();
				}
			}
		}

		protected virtual void FixedUpdate()
		{
			if (LevelSupervisor is IFixedUpdateListener updateSupervisor) {
				updateSupervisor.FixedUpdate();
			}

			foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

				if (playerContext.StatesStack?.CurrentState is IFixedUpdateListener updateState && !playerContext.StatesStack.ChangingStates) {
					updateState.FixedUpdate();
				}
			}
		}

		protected virtual void LateUpdate()
		{
			if (LevelSupervisor is ILateUpdateListener updateSupervisor) {
				updateSupervisor.LateUpdate();
			}

			foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

				if (playerContext.StatesStack?.CurrentState is ILateUpdateListener updateState && !playerContext.StatesStack.ChangingStates) {
					updateState.LateUpdate();
				}
			}

		}

#if GFRAME_ASYNC

		public async void SwitchLevelAsync(ILevelSupervisor nextLevel)
		{
			bool hadPreviousSupervisor = false;

			if (LevelSupervisor != null) {

				hadPreviousSupervisor = true;

				await UnloadingSupervisorAsync();

				if (ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
					await LevelLoadingScreen.ShowAsync();
				}

				foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

					if (playerContext.StatesStack != null && !playerContext.StatesStack.IsEmpty) {
						await playerContext.DisposePlayerStackAsync();
					}
				}


				if (!ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
					await LevelLoadingScreen.ShowAsync();
				}

				await LevelSupervisor.UnloadAsync();

				await UnloadedSupervisorAsync();

			} else if (LevelLoadingScreen != null) {
				LevelLoadingScreen.HideInstantly();
			}

			LevelSupervisor = nextLevel;

			await LoadingSupervisorAsync();

			await nextLevel.LoadAsync();

			// Avoid first show of loading screen when the game starts.
			if (hadPreviousSupervisor && LevelLoadingScreen != null) {
				await LevelLoadingScreen.HideAsync();
			}

			await LoadedSupervisorAsync();
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task UnloadingSupervisorAsync()
		{
			Debug.Log($"Unloading level supervisor {LevelSupervisor}");
			UnloadingSupervisor?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task UnloadedSupervisorAsync()
		{
			UnloadedSupervisor?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task LoadingSupervisorAsync()
		{
			Debug.Log($"Loading level supervisor {LevelSupervisor}");
			LoadingSupervisor?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task LoadedSupervisorAsync()
		{
			LoadedSupervisor?.Invoke();

			return Task.CompletedTask;
		}

		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void PushGlobalState(IPlayerState state)
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PushStateAsync(state);
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void SetLevelState(IPlayerState state)
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.SetStateAsync(state);
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void PopLevelState()
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopStateAsync();
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void PopLevelStates(int count)
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopStatesAsync(count);
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void ReenterCurrentLevelState()
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ReenterCurrentStateAsync();
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public async void ChangeLevelState(IPlayerState state, StackAction stackAction)
		{
			await PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ChangeStateAsync(state, stackAction);
		}

#else
		public void SwitchLevel(ILevelSupervisor nextLevel)
		{
			StartCoroutine(SwitchLevelCrt(nextLevel));
		}

		public IEnumerator SwitchLevelCrt(ILevelSupervisor nextLevel)
		{
			bool hadPreviousSupervisor = false;

			if (LevelSupervisor != null) {

				hadPreviousSupervisor = true;

				yield return UnloadingSupervisorCrt();

				if (ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
					yield return LevelLoadingScreen.Show();
				}

				foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

					if (playerContext.StatesStack != null && !playerContext.StatesStack.IsEmpty) {
						yield return playerContext.ClearPlayerStackCrt();
					}
				}


				if (!ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
					yield return LevelLoadingScreen.Show();
				}

				yield return LevelSupervisor.Unload();

				yield return UnloadedSupervisorCrt();

			} else if (LevelLoadingScreen != null) {
				LevelLoadingScreen.HideInstantly();
			}

			LevelSupervisor = nextLevel;

			yield return LoadingSupervisorCrt();

			yield return nextLevel.Load();

			// Avoid first show of loading screen when the game starts.
			if (hadPreviousSupervisor && LevelLoadingScreen != null) {
				yield return LevelLoadingScreen.Hide();
			}

			yield return LoadedSupervisorCrt();
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator UnloadingSupervisorCrt()
		{
			Debug.Log($"Unloading level supervisor {LevelSupervisor}");
			UnloadingSupervisor?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator UnloadedSupervisorCrt()
		{
			UnloadedSupervisor?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator LoadingSupervisorCrt()
		{
			Debug.Log($"Loading level supervisor {LevelSupervisor}");
			LoadingSupervisor?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator LoadedSupervisorCrt()
		{
			if (LevelSupervisor.StatesStack == null) {
				// In case the scopes were already active when the supervisor kicked in and it pushed
				// a new state onto the InputActionsStack (resetting the previous actions).
				// Do this only if there is no StatesStack, as it will do the same thing on setting a state.
				Input.UIScope.UIScope.RefocusActiveScopes();
			}

			LoadedSupervisor?.Invoke();

			yield break;
		}


		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// </summary>
		public void PushLevelState(ILevelState state)
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PushStateCrt(state));
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// </summary>
		public void SetLevelState(ILevelState state)
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.SetStateCrt(state));
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// </summary>
		public void PopLevelState()
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopStateCrt());
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// </summary>
		public void PopLevelStates(int count)
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopStatesCrt(count));
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// </summary>
		public void ReenterCurrentLevelState()
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ReenterCurrentStateCrt());
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// </summary>
		public void ChangeLevelState(ILevelState state, StackAction stackAction)
		{
			StartCoroutine(PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ChangeStateCrt(state, stackAction));
		}
#endif

	}
}