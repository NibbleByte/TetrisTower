using DevLocker.GFrame.SampleGame.Game;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.SampleGame.UITester
{
	/// <summary>
	/// Controller used to demonstrate how to save and load bindings.
	/// </summary>
	public class SampleUITesterBindingsController : MonoBehaviour
	{
		public void SaveBindings()
		{
			var context = SampleLevelsManager.Instance.GameContext;

			var rebinds = context.PlayerInput.actions.SaveBindingOverridesAsJson();
			PlayerPrefs.SetString("sample-game-rebinds", rebinds);

			Debug.Log($"Rebinds saved:\n{rebinds}");
		}

		public void LoadBindings()
		{
			var context = SampleLevelsManager.Instance.GameContext;

			var rebinds = PlayerPrefs.GetString("sample-game-rebinds");
			if (!string.IsNullOrEmpty(rebinds)) {
				context.PlayerInput.actions.LoadBindingOverridesFromJson(rebinds);
				context.InputContext.TriggerLastUsedDeviceChanged();
			}

			Debug.Log($"Rebinds loaded:\n{rebinds}");
		}
	}

}