using DevLocker.GFrame.Input.UIScope;

namespace DevLocker.GFrame
{
	/// <summary>
	/// Bridge between per-player input and level states.
	/// </summary>
	public static class UIPlayerRootObjectExtensions
	{
		/// <summary>
		/// Short-cut to create <see cref="LevelStateStack"/> specific to this player.
		/// </summary>
		public static void CreatePlayerStack(this UIPlayerRootObject playerRoot, params object[] references)
		{
			playerRoot.AddContextReference(new LevelStateStack(references));
		}

		/// <summary>
		/// Short-cut to get the <see cref="LevelStateStack"/> for this player.
		/// </summary>
		public static LevelStateStack GetPlayerStateStack(this UIPlayerRootObject playerRoot)
		{
			return playerRoot.GetContextReference<LevelStateStack>();
		}
	}
}