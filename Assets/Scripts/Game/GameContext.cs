using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using System.Collections;
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
	public sealed class GameContext
	{
		public GameContext(GameConfig config, PlayerControls controls)
		{
			GameConfig = config;
			PlayerControls = controls;
			PlayerControls.InitStack();

			InputContext = new SinglePlayerInputCollectionContext(PlayerControls, PlayerControls.InputStack, PlayerControls.UI.Get(), GameConfig.BindingDisplayAssets);
			InputContextManager.SetContext(InputContext);
		}

		public GameConfig GameConfig { get; }

		public PlayerOptions Options { get; } = new PlayerOptions();

		public PlayerControls PlayerControls { get; }

		public PlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private PlaythroughData m_DebugPlaythroughData;

		public IInputContext InputContext { get; }

		public void SetCurrentPlaythrough(PlaythroughTemplate playthroughTemplate)
		{
			PlaythroughData playthroughData = playthroughTemplate.GeneratePlaythroughData(GameConfig);
			SetCurrentPlaythrough(playthroughData);
		}

		public void SetCurrentPlaythrough(PlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;
		}

		public void ClearCurrentPlaythrough()
		{
			CurrentPlaythrough = m_DebugPlaythroughData = null;
		}

		public void Dispose()
		{
			InputContextManager.DisposeContext();
		}
	}
}