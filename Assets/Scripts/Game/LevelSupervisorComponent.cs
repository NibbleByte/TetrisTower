using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Game
{
	/// <summary>
	/// Holds the current level supervisor. Useful when needed by the other systems.
	/// Attach where you see fit, so the level systems can find it easily.
	/// </summary>
	public class LevelSupervisorComponent : MonoBehaviour
	{
		public ILevelSupervisor LevelSupervisor;

		public void SwitchLevel(ILevelSupervisor nextLevel)
		{
			LevelSupervisor.SwitchLevel(nextLevel);
		}

		public static void AttachTo(GameObject target, ILevelSupervisor supervisor)
		{
			target.AddComponent<LevelSupervisorComponent>().LevelSupervisor = supervisor;
		}
	}
}