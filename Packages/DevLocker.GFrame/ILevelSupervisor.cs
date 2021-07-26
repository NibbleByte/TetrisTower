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
	/// Implement this if you want to show / hide loading screen between your levels (e.g. fade out effects).
	/// Set it to the LevelsManager to be used.
	/// </summary>
	public interface ILevelLoadingScreen
	{
		IEnumerator Show();
		IEnumerator Hide();

		void ShowInstantly();
		void HideInstantly();

		bool HasShowFinished { get; }
		bool HasHideFinished { get; }
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