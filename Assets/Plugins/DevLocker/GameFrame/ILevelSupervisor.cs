using System.Collections;

namespace DevLocker.GameFrame
{
	/// <summary>
	/// Marks class to be passed onto levels as game context.
	/// </summary>
	public interface IGameContext
	{

	}

#if USE_INPUT_SYSTEM
	/// <summary>
	/// Implement this if your game uses Unity Input system with generated IInputActionCollection.
	/// </summary>
	public interface IInputActionsProvider
	{
		UnityEngine.InputSystem.IInputActionCollection2 Controls { get; }
	}
#endif

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