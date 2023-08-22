using DevLocker.GFrame.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace TetrisTower.Game
{
	public partial class @PlayerControls : IInputActionCollection2, IDisposable, IInputContext
	{
		public IInputContext InputContext { get; private set; }

		public void SetInputContext(IInputContext context)
		{
			InputContext = context;
		}

		#region IInputContext Forwarding

		public event Action LastUsedDeviceChanged {
			add { InputContext.LastUsedDeviceChanged += value; }
			remove { InputContext.LastUsedDeviceChanged -= value; }
		}

		public event Action LastUsedInputControlSchemeChanged {
			add { InputContext.LastUsedInputControlSchemeChanged += value; }
			remove { InputContext.LastUsedInputControlSchemeChanged -= value; }
		}

		public bool DeviceSupportsUINavigationSelection => InputContext.DeviceSupportsUINavigationSelection;

		public InputDevice ForcedDevice { get => InputContext.ForcedDevice; set => InputContext.ForcedDevice = value; }

		public InputActionsMaskedStack InputActionsMaskedStack => InputContext.InputActionsMaskedStack;

		public InputAction FindActionFor(string actionNameOrId, bool throwIfNotFound = false) => InputContext.FindActionFor(actionNameOrId, throwIfNotFound);
		public void EnableAction(object source, InputAction action) => InputContext.EnableAction(source, action);
		public void DisableAction(object source, InputAction action) => InputContext.DisableAction(source, action);
		public void DisableAll(object source) => InputContext.DisableAll(source);
		public IEnumerable<InputAction> GetInputActionsEnabledBy(object source) => InputContext.GetInputActionsEnabledBy(source);

		public void PushOrSetActionsMask(object source, IEnumerable<InputAction> actionsMask, bool setBackToTop = false) => InputContext.PushOrSetActionsMask(source, actionsMask, setBackToTop);
		public void PopActionsMask(object source) => InputContext.PopActionsMask(source);

		public IEnumerable<InputAction> GetUIActions() => InputContext.GetUIActions();
		public IEnumerable<InputAction> GetAllActions() => InputContext.GetAllActions();
		public ReadOnlyArray<InputDevice> GetPairedInputDevices() => InputContext.GetPairedInputDevices();

		public InputDevice GetLastUsedInputDevice() => InputContext.GetLastUsedInputDevice();
		public InputControlScheme GetLastUsedInputControlScheme() => InputContext.GetLastUsedInputControlScheme();
		public void TriggerLastUsedDeviceChanged() => InputContext.TriggerLastUsedDeviceChanged();
		public void TriggerLastUsedInputControlSchemeChanged() => InputContext.TriggerLastUsedInputControlSchemeChanged();


		public IEnumerable<InputControlScheme> GetAllInputControlSchemes() => InputContext.GetAllInputControlSchemes();
		public IReadOnlyList<IInputBindingDisplayDataProvider> GetAllDisplayDataProviders() => InputContext.GetAllDisplayDataProviders();
		public IInputBindingDisplayDataProvider GetCurrentDisplayDataProvider() => InputContext.GetCurrentDisplayDataProvider();

		#endregion
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

		public IPlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private IPlaythroughData m_DebugPlaythroughData;

		public IInputContext InputContext { get; }

		public void SetCurrentPlaythrough(PlaythroughTemplateBase playthroughTemplate)
		{
			IPlaythroughData playthroughData = playthroughTemplate.GeneratePlaythroughData(GameConfig);
			SetCurrentPlaythrough(playthroughData);
		}

		public void SetCurrentPlaythrough(IPlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;
		}

		public void ClearCurrentPlaythrough()
		{
			CurrentPlaythrough = m_DebugPlaythroughData = null;
		}
	}
}