using System.Collections;

namespace DevLocker.GFrame
{
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

		IEnumerator Load();

		IEnumerator Unload();
	}

	/// <summary>
	/// Use this interface in your supervisors to notify your scene behaviours and controllers that the level has finished loading or will be unloading.
	/// This interface is optional and you can make another one that suits your needs.
	/// </summary>
	public interface ILevelLoadListener
	{
		void OnLevelLoaded(LevelStateContextReferences contextReferences);
		void OnLevelUnloading();
	}

	/// <summary>
	/// Your level supervisor or level state can implement this to get invoked on Unity update.
	/// </summary>
	public interface IUpdateListener
	{
		void Update();
	}

	/// <summary>
	/// Your level supervisor or level state can implement this to get invoked on Unity fixed update.
	/// </summary>
	public interface IFixedUpdateListener
	{
		void FixedUpdate();
	}

	/// <summary>
	/// Your level supervisor or level state can implement this to get invoked on Unity late update.
	/// </summary>
	public interface ILateUpdateListener
	{
		void LateUpdate();
	}
}