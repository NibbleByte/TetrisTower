using System.Collections;

namespace TetrisTower.Game
{
	public interface ILevelSupervisor
	{
		GameContext GameContext { get; }

		IEnumerator Load();

		IEnumerator Unload();
	}

	public static class LevelSupervisorUtils
	{
		public static void SwitchLevel(this ILevelSupervisor prevLevel, ILevelSupervisor nextLevel)
		{
			prevLevel.GameContext.StartCoroutine(SwitchLevelCrt(prevLevel, nextLevel));
		}

		private static IEnumerator SwitchLevelCrt(ILevelSupervisor prevLevel, ILevelSupervisor nextLevel)
		{
			yield return prevLevel.Unload();

			yield return nextLevel.Load();
		}
	}

}