using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.Game
{
	public partial class @SamplePlayerControls : IInputActionCollection2, IDisposable
	{
		public InputActionsStack InputStack { get; private set; }

		public void InitStack()
		{
			InputStack = new InputActionsStack(this);
		}
	}

	public sealed class SampleGameContext : IGameContext, IInputContextProvider
	{
		public SampleGameContext(PlayerInput playerInput, SamplePlayerControls controls, IEnumerable<IInputBindingDisplayDataProvider> bindingDisplayProviders)
		{
			PlayerInput = playerInput;
			PlayerControls = controls;
			PlayerControls.InitStack();

			InputContext = new SinglePlayerInputComponentContext(PlayerInput, PlayerControls.InputStack, bindingDisplayProviders);
		}

		public SamplePlayerControls PlayerControls { get; }

		public PlayerInput PlayerInput { get; }

		public IInputContext InputContext { get; }

		public void Dispose()
		{
			InputContext.Dispose();
		}
	}
}