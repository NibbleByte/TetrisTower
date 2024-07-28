using DevLocker.GFrame;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Game
{
	public class GameManager : LevelsManager
	{
		public static GameManager Instance { get; private set; }

		public GameContext GameContext { get; private set; }

		private List<object> m_Managers;

		public T GetManager<T>() => m_Managers.OfType<T>().First();

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

		public void SetGameContext(GameContext gameContext) => GameContext = gameContext;

		public void SetManagers(params object[] managers) => m_Managers = new List<object>(managers);

		public void AppendManager(object manager) => m_Managers.Add(manager);
	}
}