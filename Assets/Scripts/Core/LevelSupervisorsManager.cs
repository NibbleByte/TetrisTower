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
		public ILevelSupervisor LevelSupervisor { get; private set; }

		public T CastSupervisor<T>() => (T) LevelSupervisor;

		public static LevelSupervisorsManager Instance { get; private set; }

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(gameObject);
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


		public void SwitchLevel(ILevelSupervisor nextLevel)
		{
			StartCoroutine(SwitchLevelCrt(nextLevel));
		}

		public IEnumerator SwitchLevelCrt(ILevelSupervisor nextLevel)
		{
			if (LevelSupervisor != null) {
				Debug.Log($"Unloading level supervisor {LevelSupervisor}");
				yield return LevelSupervisor.Unload();
			}

			LevelSupervisor = nextLevel;

			Debug.Log($"Loading level supervisor {nextLevel}");
			yield return nextLevel.Load();
		}
	}
}