using DevLocker.GameFrame;
using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Game
{
	[Serializable]
	public class GameContext : IGameContext, IInputActionsProvider
	{
		public GameContext(GameConfig config, PlayerControls controls, CoroutineScheduler coroutineScheduler)
		{
			GameConfig = config;
			PlayerControls = controls;

			CoroutineScheduler = coroutineScheduler;
		}

		public GameConfig GameConfig { get; }

		public IInputActionCollection2 Controls => PlayerControls;

		public PlayerControls PlayerControls { get; }

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
}