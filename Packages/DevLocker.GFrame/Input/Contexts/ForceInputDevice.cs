#if USE_INPUT_SYSTEM
using DevLocker.GFrame.Input.UIInputDisplay;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DevLocker.GFrame.Input.Contexts
{
	/// <summary>
	/// Add this next to <see cref="PlayerContextUIRootObject"/> to force selected device to be used.
	/// This will force only hotkey icons to display for it.
	/// </summary>
	public class ForceInputDevice : MonoBehaviour
	{
		[Tooltip("If empty will use the global player context.")]
		public PlayerContextUIRootObject PlayerContext;

		public InputBindingDisplayAsset ForcedDevice;

		private InputBindingDisplayAsset m_LastForcedDevice;

		private InputDevice m_FakedDevice;

		void OnDestroy()
		{
			ForcedDevice = null;
			m_LastForcedDevice = null;

			if (m_FakedDevice != null) {
				InputSystem.RemoveDevice(m_FakedDevice);
				m_FakedDevice = null;
			}
		}

		void Update()
		{
			if (PlayerContext == null) {
				PlayerContext = PlayerContextUIRootObject.GlobalPlayerContext;
			}

			if (PlayerContext == null || !PlayerContext.IsActive || PlayerContext.InputContext == null)
				return;

			if (m_LastForcedDevice != ForcedDevice) {
				m_LastForcedDevice = ForcedDevice;

				if (m_FakedDevice != null) {
					InputSystem.RemoveDevice(m_FakedDevice);
					m_FakedDevice = null;
				}

				if (m_LastForcedDevice == null) {
					PlayerContext.InputContext.ForcedDevice = null;
					return;
				}

				foreach(string layout in m_LastForcedDevice.MatchingDeviceLayouts) {
					InputDevice device = InputSystem.GetDevice(layout);
					if (device != null) {
						PlayerContext.InputContext.ForcedDevice = device;
						return;
					}
				}

				PlayerContext.InputContext.ForcedDevice =
					m_FakedDevice = InputSystem.AddDevice(m_LastForcedDevice.MatchingDeviceLayouts.First(), $"{nameof(ForceInputDevice)} - {m_LastForcedDevice.name}");
			}
		}
	}
}
#endif