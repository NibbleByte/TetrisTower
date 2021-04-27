using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Game
{
	[Serializable]
	public class GameContext
	{
		public GameContext(GameConfig config, PlayerControls controls, PlayerInput input, CoroutineScheduler coroutineScheduler)
		{
			GameConfig = config;
			PlayerControls = controls;
			PlayerInput = input;

			m_CoroutineScheduler = coroutineScheduler;
		}

		public GameConfig GameConfig { get; }

		public PlayerControls PlayerControls { get; }
		public PlayerInput PlayerInput { get; }

		public PlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private PlaythroughData m_DebugPlaythroughData;

		private readonly CoroutineScheduler m_CoroutineScheduler;

		public void SetCurrentPlaythrough(PlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;
		}

		public Coroutine StartCoroutine(IEnumerator routine)
		{
			return m_CoroutineScheduler.StartCoroutine(routine);
		}
	}
}