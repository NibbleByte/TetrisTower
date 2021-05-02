using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Core
{
	/// <summary>
	/// Contains the currently active level supervisor and can switch it to another.
	/// </summary>
	public class LevelSupervisorsManager : MonoBehaviour
	{
		public IGameContext GameContext { get; private set; }

		public ILevelSupervisor LevelSupervisor { get; private set; }
		public LevelStateStack LevelStatesStack => LevelSupervisor?.StatesStack;

		public T CastSupervisor<T>() => (T) LevelSupervisor;

		public static LevelSupervisorsManager Instance { get; private set; }

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(this);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);
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
			if (LevelSupervisor != null) {
				if (!LevelStatesStack.IsEmpty) {
					LevelStatesStack.ClearStackAndState();
				}

				Debug.Log($"Unloading level supervisor {LevelSupervisor}");
				yield return LevelSupervisor.Unload();
			}

			LevelSupervisor = nextLevel;

			Debug.Log($"Loading level supervisor {nextLevel}");
			yield return nextLevel.Load(GameContext);
		}
	}
}