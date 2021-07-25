#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.UIInputDisplay
{
	/// <summary>
	/// Activates objects when specified control scheme is active, deactivates the rest.
	/// Useful to change UI layout when switching from keyboard to gamepad and vice versa.
	/// </summary>
	public class InputControlSchemeActivatorUI : MonoBehaviour
	{
		[Serializable]
		public struct ControlSchemeActiveObjects
		{
			[Tooltip("Control scheme used in your .inputactions asset.")]
			[InputControlSchemePicker]
			public string ControlScheme;

			[NonReorderable]
			public GameObject[] Objects;
		}

		[Tooltip("Which player should this hotkey be displayed for?\nIf unsure or for single player games, leave MasterPlayer.")]
		public PlayerIndex Player = PlayerIndex.MasterPlayer;

		public ControlSchemeActiveObjects[] ControlSchemeObjects;

		private string m_LastControlScheme;

		private void RefreshObjects(IInputContext context, PlayerIndex playerIndex)
		{
			InputControlScheme scheme = context.GetLastUsedInputControlScheme(playerIndex);

			if (scheme.bindingGroup == m_LastControlScheme)
				return;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				SetObjectsActive(m_LastControlScheme, false);
			}

			m_LastControlScheme = scheme.bindingGroup;

			if (!string.IsNullOrEmpty(m_LastControlScheme)) {
				SetObjectsActive(m_LastControlScheme, true);
			}
		}

		private void SetObjectsActive(string controlScheme, bool active)
		{
			foreach(ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				if (bind.ControlScheme.Equals(controlScheme, StringComparison.OrdinalIgnoreCase)) {
					foreach(GameObject obj in bind.Objects) {
						if (obj) {
							obj.SetActive(active);
						}
					}
					return;
				}
			}
		}

		void Awake()
		{
			foreach(ControlSchemeActiveObjects bind in ControlSchemeObjects) {
				foreach(GameObject obj in bind.Objects) {
					if (obj) {
						obj.SetActive(false);
					}
				}
			}
		}

		void OnEnable()
		{
			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			context.LastUsedDeviceChanged += OnLastUsedDeviceChanged;
			m_LastControlScheme = null;
			RefreshObjects(context, Player);
		}

		void OnDisable()
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			context.LastUsedDeviceChanged -= OnLastUsedDeviceChanged;
		}

		private void OnLastUsedDeviceChanged(PlayerIndex playerIndex)
		{
			// Turning off Play mode.
			if (LevelsManager.Instance == null)
				return;

			var context = (LevelsManager.Instance.GameContext as IInputContextProvider)?.InputContext;

			if (context == null) {
				Debug.LogWarning($"{nameof(InputControlSchemeActivatorUI)} {name} can't be used if Unity Input System is not provided.", this);
				enabled = false;
				return;
			}

			if (Player == PlayerIndex.MasterPlayer) {
				if (!context.IsMasterPlayer(playerIndex))
					return;
			} else if (playerIndex != Player) {
				return;
			}

			RefreshObjects(context, playerIndex);
		}

		void OnValidate()
		{
			if (ControlSchemeObjects.SelectMany(bind => bind.Objects).Any(obj => obj && transform.IsChildOf(obj.transform))) {
				Debug.LogError($"{nameof(InputControlSchemeActivatorUI)} deactivates game objects that are parents of it. This is not allowed.", this);
			}

			if (Player == PlayerIndex.AnyPlayer) {
				Debug.LogError($"{nameof(InputControlSchemeActivatorUI)} doesn't allow setting {nameof(PlayerIndex.AnyPlayer)} for {nameof(Player)}.", this);
				Player = PlayerIndex.MasterPlayer;
#if UNITY_EDITOR
				UnityEditor.EditorUtility.SetDirty(this);
#endif
			}
		}
	}

}
#endif