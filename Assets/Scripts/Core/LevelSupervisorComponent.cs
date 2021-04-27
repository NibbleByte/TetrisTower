using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Core
{
	/// <summary>
	/// Holds the current level supervisor. Useful when needed by the other systems and for switching levels.
	/// Attach where you see fit, so the level systems can find it easily.
	/// </summary>
	public class LevelSupervisorComponent : MonoBehaviour
	{
		public ILevelSupervisor LevelSupervisor;

		// Set this if you want the switching to happen on your scheduler.
		// Make sure it will survive any scene loading.
		[NonSerialized]
		public CoroutineScheduler SwitchScheduler;

		public T CastSupervisor<T>() => (T) LevelSupervisor;

		public void SwitchLevel(ILevelSupervisor nextLevel)
		{
			if (SwitchScheduler) {
				SwitchScheduler.StartCoroutine(SwitchLevelCrt(LevelSupervisor, nextLevel, null));

			} else {
				// HACK: Use it to survive the scene reload.
				var scheduler = new GameObject("LevelSupervisor Transition").AddComponent<CoroutineScheduler>();
				DontDestroyOnLoad(scheduler.gameObject);

				scheduler.StartCoroutine(SwitchLevelCrt(LevelSupervisor, nextLevel, scheduler));
			}
		}

		private static IEnumerator SwitchLevelCrt(ILevelSupervisor prevLevel, ILevelSupervisor nextLevel, CoroutineScheduler scheduler)
		{
			Debug.Log($"Unloading level supervisor {prevLevel}");
			yield return prevLevel.Unload();

			Debug.Log($"Loading level supervisor {nextLevel}");
			yield return nextLevel.Load();

			if (scheduler) {
				GameObject.Destroy(scheduler.gameObject);
			}
		}

		public static void AttachTo(GameObject target, ILevelSupervisor supervisor)
		{
			target.AddComponent<LevelSupervisorComponent>().LevelSupervisor = supervisor;
		}
	}
}