using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Game
{
	public interface IGameContextProvider
	{
		GameContext GameContext { get; }
	}

	[Serializable]
	public class GameContext
	{
		public GameContext(GameConfig config, PlayerControls controls, PlayerInput input, CoroutineScheduler coroutineScheduler)
		{
			GameConfig = config;
			PlayerControls = controls;
			PlayerInput = input;

			CoroutineScheduler = coroutineScheduler;
		}

		public GameConfig GameConfig { get; }

		public PlayerControls PlayerControls { get; }
		public PlayerInput PlayerInput { get; }

		public PlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private PlaythroughData m_DebugPlaythroughData;

		public CoroutineScheduler CoroutineScheduler { get; }

		public void SetCurrentPlaythrough(PlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;
		}

		public Coroutine StartCoroutine(IEnumerator routine)
		{
			return CoroutineScheduler.StartCoroutine(routine);
		}
	}

	public static class GameUtils
	{
		public static GameContext GetGameContext(this LevelSupervisorsManager supervisorsManager)
		{
			return supervisorsManager.CastSupervisor<IGameContextProvider>().GameContext;
		}
	}
}