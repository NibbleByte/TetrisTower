using DevLocker.GFrame.Input;
using System;
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
		public GameContext(GameConfig config, PlayerControls controls, IInputContext inputContext)
		{
			GameConfig = config;
			PlayerControls = controls;
			InputContext = inputContext;
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
	}
}