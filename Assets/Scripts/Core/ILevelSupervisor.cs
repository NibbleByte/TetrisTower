using System.Collections;

namespace TetrisTower.Core
{
	/// <summary>
	/// Controls the whole level: loading, unloading, switching states (via the StatesStack).
	/// </summary>
	public interface ILevelSupervisor
	{
		LevelStateStack StatesStack { get; }

		IEnumerator Load();

		IEnumerator Unload();
	}

}