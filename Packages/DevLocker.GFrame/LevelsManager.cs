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
			if (m_LevelStatesStack != null) {

				yield return UnloadingSupervisorCrt();

				if (!m_LevelStatesStack.IsEmpty) {
					yield return m_LevelStatesStack.ClearStackAndStateCrt();
				}

				yield return LevelSupervisor.Unload();

				yield return UnloadedSupervisorCrt();
			}

			LevelSupervisor = nextLevel;

			yield return LoadingSupervisorCrt();

			yield return nextLevel.Load(GameContext);

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