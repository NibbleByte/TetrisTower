using System.Collections;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Marks class to be passed onto levels as game context.
	/// </summary>
	public interface IGameContext
	{

	}

	/// <summary>
	/// Controls the whole level: loading, unloading, switching states (via the StatesStack).
	/// </summary>
	public interface ILevelSupervisor
	{
		LevelStateStack StatesStack { get; }

		IEnumerator Load(IGameContext gameContext);

		IEnumerator Unload();
	}

}