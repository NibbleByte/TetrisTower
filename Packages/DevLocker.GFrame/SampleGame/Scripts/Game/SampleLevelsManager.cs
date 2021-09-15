using UnityEngine;

namespace DevLocker.GFrame.SampleGame.Game
{
	/// <summary>
	/// LevelsManager is the center of your game.
	/// They take care of the levels switching.
	/// They are also good candidate for singleton entry point of your game - add the game context on it.
	/// </summary>
	public class SampleLevelsManager : LevelsManager
	{
		public static SampleLevelsManager Instance { get; private set; }

		public SampleGameContext GameContext { get; private set; }

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(this);
				return;
			}

			Instance = this;

			if (transform.parent == null) {
				DontDestroyOnLoad(gameObject);
			}
		}

		void OnDestroy()
		{
			if (Instance == this) {
				Instance = null;
			}
		}

		public void SetGameContext(SampleGameContext gameContext)
		{
			GameContext = gameContext;
		}
	}
}