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

		public static LevelsManager Instance { get; private set; }

		void Awake()
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

		void OnDestroy()
		{
			if (Instance == this) {
				Instance = null;
			}
		}

		public void SetGameContext(IGameContext gameContext)
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
				if (!m_LevelStatesStack.IsEmpty) {
					yield return m_LevelStatesStack.ClearStackAndStateCrt();
				}

				Debug.Log($"Unloading level supervisor {LevelSupervisor}");
				yield return LevelSupervisor.Unload();
			}

			LevelSupervisor = nextLevel;

			Debug.Log($"Loading level supervisor {nextLevel}");
			yield return nextLevel.Load(GameContext);
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