using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.UIInputDisplay;
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
		}
	}

	[Serializable]
	public sealed class GameContext : IGameContext, IInputContextProvider
	{
		public GameContext(GameConfig config, PlayerControls controls, CoroutineScheduler coroutineScheduler)
		{
			GameConfig = config;
			PlayerControls = controls;
			PlayerControls.InitStack();

			CoroutineScheduler = coroutineScheduler;

			InputContext = new SinglePlayerInputCollectionContext(PlayerControls, PlayerControls.InputStack, PlayerControls.UI.Get(), GameConfig.BindingDisplayAssets);
		}

		public GameConfig GameConfig { get; }

		public PlayerControls PlayerControls { get; }

		public PlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private PlaythroughData m_DebugPlaythroughData;

		public CoroutineScheduler CoroutineScheduler { get; }

		public IInputContext InputContext { get; }

		public void SetCurrentPlaythrough(PlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;
		}

		public Coroutine StartCoroutine(IEnumerator routine)
		{
			return CoroutineScheduler.StartCoroutine(routine);
		}

		public void Dispose()
		{
			InputContext.Dispose();
		}
	}
}