using System.Collections;
using System.Threading.Tasks;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Implement this if you want to show / hide loading screen between your levels (e.g. fade out effects).
	/// Set it to the LevelsManager to be used.
	/// </summary>
	public interface ILevelLoadingScreen
	{
#if GFRAME_ASYNC
		Task ShowAsync();
		Task HideAsync();
#else
		IEnumerator Show();
		IEnumerator Hide();
#endif

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

#if GFRAME_ASYNC
		Task LoadAsync();

		Task UnloadAsync();
#else
		IEnumerator Load();

		IEnumerator Unload();
#endif
	}

	/// <summary>
	/// Use this interface in your supervisors to notify your scene behaviours and controllers that the level is currently loading.
	/// The supervisor should wait on the <see cref="OnLevelLoading(LevelStateContextReferences)"/>, as the behaviours can loading on their own.
	/// This interface is optional and you can make another one that suits your needs.
	/// </summary>
	public interface ILevelLoadingListener
	{
#if GFRAME_ASYNC
		Task OnLevelLoadingAsync(LevelStateContextReferences contextReferences);
#else
		IEnumerator OnLevelLoading(LevelStateContextReferences contextReferences);
#endif
	}

	/// <summary>
	/// Use this interface in your supervisors to notify your scene behaviours and controllers that the level has finished loading or will be unloading.
	/// This interface is optional and you can make another one that suits your needs.
	/// </summary>
	public interface ILevelLoadedListener
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

	/// <summary>
	/// Bridge between per-player input and level states.
	/// </summary>
	public static class LevelPlayerContextExtensions
	{
		/// <summary>
		/// Short-cut to create <see cref="LevelStateStack"/> specific to this player.
		/// </summary>
		public static void CreatePlayerStack(this Input.Contexts.PlayerContextUIRootObject playerRoot, params object[] references)
		{
			playerRoot.AddContextReference(new LevelStateStack(references));
		}

		/// <summary>
		/// Short-cut to get the <see cref="LevelStateStack"/> for this player.
		/// </summary>
		public static LevelStateStack GetPlayerStateStack(this Input.IPlayerContext playerRoot)
		{
			return playerRoot.GetContextReference<LevelStateStack>();
		}
	}
}