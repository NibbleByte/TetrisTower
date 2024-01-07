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

		/// <summary>
		/// Current level supervisor.
		/// </summary>
		public ILevelSupervisor LevelSupervisor { get; private set; }

		/// <summary>
		/// Is level currently changing. Can't start another change while this is true.
		/// Initially set to true as there is no level loaded and it is expected to load.
		/// </summary>
		public bool ChangingLevel { get; private set; } = true;

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

		#region Global Player State
		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void PushGlobalState(IPlayerState state)
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PushState(state);
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void SetGlobalState(IPlayerState state)
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.SetState(state);
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void PopGlobalState()
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopState();
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void PopGlobalStates(int count)
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.PopStates(count);
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void ReenterCurrentGlobalState()
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ReenterCurrentState();
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// Works with the <see cref="PlayerContextUIRootObject.GlobalPlayerContext"/>. Don't use in split-screen games.
		/// </summary>
		public void ChangeGlobalState(IPlayerState state, StackAction stackAction)
		{
			PlayerContextUIRootObject.GlobalPlayerContext.StatesStack.ChangeState(state, stackAction);
		}

		#endregion


#if GFRAME_ASYNC

		public async void SwitchLevelAsync(ILevelSupervisor nextLevel)
		{
			if (ChangingLevel && LevelSupervisor != null) {
				throw new InvalidOperationException($"Level is already changing. Can't switch to {nextLevel} while change is in progress.");
			}

			ChangingLevel = true;
			ILevelSupervisor prevLevel = LevelSupervisor;

			foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {
				playerContext.IsLevelLoading = true;
			}

			try {

				if (LevelSupervisor != null) {

					if (ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
						await LevelLoadingScreen.ShowAsync();
					}

					await UnloadingSupervisorAsync();

					foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

						if (playerContext.StatesStack != null) {
							playerContext.DisposePlayerStack();
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
				if (prevLevel != null && LevelLoadingScreen != null) {

					// Wait 1 frame for performance to stabilize (or transition animations will be skipped).
					await Task.Yield();

					await LevelLoadingScreen.HideAsync();
				}

				await LoadedSupervisorAsync();

				ChangingLevel = false;
			}
			catch (Exception ex) {
				ChangingLevel = false;

				if (!OnException(prevLevel, nextLevel, ex)) {
					throw;
				}

			} finally {

				foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {
					playerContext.IsLevelLoading = false;
				}
			}
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual Task UnloadingSupervisorAsync()
		{
			Debug.Log($"[GFrame] Unloading level supervisor {LevelSupervisor}");
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
			Debug.Log($"[GFrame] Loading level supervisor {LevelSupervisor}");
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
		/// Chance to handle exceptions on level switching. Return true if handled.
		/// Example: switch to another fall-back level.
		/// </summary>
		protected virtual bool OnException(ILevelSupervisor prevLevel, ILevelSupervisor nextLevel, Exception exception)
		{
			return false;
		}

#else
		/// <summary>
		/// If coroutines fail, use this to reset the changing level flag so you can switch again. For example switch to fall-back level.
		/// </summary>
		public void RestartChangingLevelFlag() => ChangingLevel = false;

		public void SwitchLevel(ILevelSupervisor nextLevel)
		{
			StartCoroutine(SwitchLevelCrt(nextLevel));
		}

		public IEnumerator SwitchLevelCrt(ILevelSupervisor nextLevel)
		{
			if (ChangingLevel && LevelSupervisor != null) {
				throw new InvalidOperationException($"Level is already changing. Can't switch to {nextLevel} while change is in progress.");
			}

			// If exception happens in some of the coroutines, flag will remain set forever.
			// Use the RestartChangingLevelFlag() to restore it and switch back to fall-back level.
			ChangingLevel = true;

			bool hadPreviousSupervisor = false;

			if (LevelSupervisor != null) {

				hadPreviousSupervisor = true;

				yield return UnloadingSupervisorCrt();

				if (ShowLoadingScreenBeforeLevelStates && LevelLoadingScreen != null) {
					yield return LevelLoadingScreen.Show();
				}

				foreach (PlayerContextUIRootObject playerContext in PlayerContextUIRootObject.AllPlayerUIRoots) {

					if (playerContext.StatesStack != null) {
						playerContext.DisposePlayerStack();
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

				// Wait 1 frame for performance to stabilize (or transition animations will be skipped).
				yield return null;

				yield return LevelLoadingScreen.Hide();
			}

			yield return LoadedSupervisorCrt();

			ChangingLevel = false;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator UnloadingSupervisorCrt()
		{
			Debug.Log($"[GFrame] Unloading level supervisor {LevelSupervisor}");
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
			Debug.Log($"[GFrame] Loading level supervisor {LevelSupervisor}");
			LoadingSupervisor?.Invoke();

			yield break;
		}

		/// <summary>
		/// Override this according to your needs.
		/// </summary>
		protected virtual IEnumerator LoadedSupervisorCrt()
		{
			LoadedSupervisor?.Invoke();

			yield break;
		}

#endif

	}
}