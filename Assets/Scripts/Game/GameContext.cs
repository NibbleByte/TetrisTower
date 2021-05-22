using DevLocker.GFrame;
using System;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Game
{
	public partial class @PlayerControls : IInputActionCollection2, IDisposable
	{
		public InputActionsStack InputStack { get; private set; }

		public void InitStack()
		{
			InputStack = new InputActionsStack(this);

			// Make sure no input is enabled when starting level (including UI).
			Disable();
		}
	}

	[Serializable]
	public class GameContext : IGameContext, IInputActionsContext
	{
		public GameContext(GameConfig config, PlayerControls controls, CoroutineScheduler coroutineScheduler)
		{
			GameConfig = config;
			PlayerControls = controls;
			PlayerControls.InitStack();

			CoroutineScheduler = coroutineScheduler;
		}

		public GameConfig GameConfig { get; }

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


		#region IInputActionsContext

		public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
		{
			return PlayerControls.FindAction(actionNameOrId, throwIfNotFound);
		}

		public void PushActionsState(object source, bool resetActions = true)
		{
			PlayerControls.InputStack.PushActionsState(source, resetActions);
		}

		public bool PopActionsState(object source)
		{
			return PlayerControls.InputStack.PopActionsState(source);
		}

		public IEnumerable<InputAction> GetUIActions()
		{
			return PlayerControls.UI.Get();
		}

		#endregion
	}
}