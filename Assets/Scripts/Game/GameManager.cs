using DevLocker.GFrame;
using UnityEngine;

namespace TetrisTower.Game
{
	public class GameManager : LevelsManager
	{
		public static GameManager Instance { get; private set; }

		public GameContext GameContext { get; private set; }

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

		public void SetGameContext(GameContext gameContext)
		{
			GameContext = gameContext;
		}
	}
}