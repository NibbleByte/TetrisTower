using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Contains the currently active level supervisor and can switch it to another.
	/// </summary>
	public class LevelsManager : MonoBehaviour
	{
		public IGameContext GameContext { get; private set; }

		/// <summary>
		/// Assign this if you want to show / hide loading screen between your levels (e.g. fade out effects).
		/// You may assign this multiple times based on your needs and next level to load.
		/// If null, it will be skipped.
		/// </summary>
		public ILevelLoadingScreen LevelLoadingScreen;

		[Tooltip("Should level loading screen show before exiting the level states or after.")]
		public bool ShowLoadingScreenBeforeLevelStates = false;

		public ILevelSupervisor LevelSupervisor { get; private set; }
		private LevelStateStack m_LevelStatesStack => LevelSupervisor?.StatesStack;

		// Listen for supervisor change.
		// NOTE: avoid using events with more complex logic as it will blow up in your face.
		//		 If you really need to do it, you can inherit this LevelsManager and override the corresponding protected methods.
		public event Action UnloadingSupervisor;
		public event Action UnloadedSupervisor;
		public event Action LoadingSupervisor;
		public event Action LoadedSupervisor;

		public static LevelsManager Instance { get; private set; }

		protected virtual void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(this);
				return;
			}

			Instance = this;

			if (transform.parent == null) {
				DontDestroyOnLoad(gameObject);
			}
		}

		protected virtual void OnDestroy()
		{
			if (Instance == this) {
				Instance = null;
			}
		}

		protected virtual void Update()
		{
			if (GameContext is IUpdateListener updateContext) {
				updateContext.Update();
			}

			if (LevelSupervisor is IUpdateListener updateSupervisor) {
				updateSupervisor.Update();
			}

			if (m_LevelStatesStack?.CurrentState is IUpdateListener updateState) {
				updateState.Update();
			}
		}

		protected virtual void LateUpdate()
		{
			if (GameContext is ILateUpdateListener lateUpdateContext) {
				lateUpdateContext.LateUpdate();
			}

			if (LevelSupervisor is ILateUpdateListener updateSupervisor) {
				updateSupervisor.LateUpdate();
			}

			if (m_LevelStatesStack?.CurrentState is ILateUpdateListener updateState) {
				updateState.LateUpdate();
			}
		}

		public virtual void SetGameContext(IGameContext gameContext)
		{
			GameContext = gameContext;
		}


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

				if (m_LevelStatesStack != null && !m_LevelStatesStack.IsEmpty) {
					yield return m_LevelStatesStack.ClearStackAndStateCrt();
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

			yield return nextLevel.Load(GameContext);

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
				UIScope.UIScope.RefocusActiveScopes();
			}

			LoadedSupervisor?.Invoke();

			yield break;
		}


		/// <summary>
		/// Push state to the top of the state stack. Can pop it out to the previous state later on.
		/// </summary>
		public void PushLevelState(ILevelState state)
		{
			StartCoroutine(m_LevelStatesStack.PushStateCrt(state));
		}

		/// <summary>
		/// Clears the state stack of any other states and pushes the provided one.
		/// </summary>
		public void SetLevelState(ILevelState state)
		{
			StartCoroutine(m_LevelStatesStack.SetStateCrt(state));
		}

		/// <summary>
		/// Pop a single state from the state stack.
		/// </summary>
		public void PopLevelState()
		{
			StartCoroutine(m_LevelStatesStack.PopStateCrt());
		}

		/// <summary>
		/// Pops multiple states from the state stack.
		/// </summary>
		public void PopLevelStates(int count)
		{
			StartCoroutine(m_LevelStatesStack.PopStatesCrt(count));
		}

		/// <summary>
		/// Pop and push back the state at the top. Will trigger changing state events.
		/// </summary>
		public void ReenterCurrentLevelState()
		{
			StartCoroutine(m_LevelStatesStack.ReenterCurrentStateCrt());
		}

		/// <summary>
		/// Exits the state and leaves the stack empty.
		/// </summary>
		public void ClearLevelStackAndState()
		{
			StartCoroutine(m_LevelStatesStack.ClearStackAndStateCrt());
		}

		/// <summary>
		/// Change the current state and add it to the state stack.
		/// Will notify the state itself.
		/// Any additional state changes that happened in the meantime will be queued and executed after the current change finishes.
		/// </summary>
		public void ChangeLevelState(ILevelState state, StackAction stackAction)
		{
			StartCoroutine(m_LevelStatesStack.ChangeStateCrt(state, stackAction));
		}
	}
}