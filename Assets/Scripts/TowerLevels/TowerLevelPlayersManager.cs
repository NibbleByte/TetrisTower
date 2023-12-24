using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.MessageBox;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Replays;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelPlayersManager
	{
		private IPlaythroughData m_PlaythroughData;

		public TowerLevelPlayersManager(IPlaythroughData playthroughData)
		{
			m_PlaythroughData = playthroughData;
		}

		public PlaythroughPlayer SetupPlayer(GameConfig config, GridLevelController levelController, GridLevelData levelData, Camera camera, PlayerContextUIRootObject playerContextRoot)
		{
			var playthroughPlayer = PlaythroughPlayer.Create(config, levelController, camera, playerContextRoot);
			m_PlaythroughData.AssignPlayer(playthroughPlayer, levelData);

			return playthroughPlayer;
		}

		public void Dispose()
		{
			MessageBox.Instance.MessageShown -= SystemPauseLevels;
			MessageBox.Instance.MessageClosed -= SystemResumeLevels;

			foreach (var player in m_PlaythroughData.ActivePlayers) {
				player.EventSystem.gameObject.SetActive(false);
			}

			// Restore the global input.
			//PlayerContextUIRootObject.GlobalPlayerContext.EventSystem.gameObject.SetActive(true);

			m_PlaythroughData = null;
		}

		public void SetupPlayersInput()
		{
			MessageBox.Instance.MessageShown += SystemPauseLevels;
			MessageBox.Instance.MessageClosed += SystemResumeLevels;

			// Suppress global input.
			//PlayerContextUIRootObject.GlobalPlayerContext.EventSystem.gameObject.SetActive(false);

			var players = m_PlaythroughData.ActivePlayers.ToList();

			// For single player do nothing - use all the input devices.
			if (players.Count > 1) {
				// Assign devices per player (only if multiple)
				// Helpful table (K - keyboard, M - mouse, C* - controller, X - no device available, P* player):
				//	P1		P2	P3	P4
				//	K		M	C1	C2
				//	KM		C1	C2	C3
				//	KMC1	C2	C3	C4
				//	K		M	C1	X
				//	M		C1	C2	X
				//	C1		C2	C3	X

				bool hasKeyboard = Keyboard.current != null && Keyboard.current.enabled;
				bool hasMouse = Mouse.current != null && Mouse.current.enabled;

				var gamepads = Gamepad.all.Where(g => g.enabled).ToList();
				if (gamepads.Count > 0) {

					int pcDevicesPaired;

					// First player always gets the keyboard always.
					if (hasKeyboard) {
						players[0].InputContext.PerformPairingWithDevice(Keyboard.current);

						if (hasMouse) {
							// If controllers are enough, give the mouse to the first player as well. Else give it to the second.
							if (players.Count - gamepads.Count <= 1) {
								players[0].InputContext.PerformPairingWithDevice(Mouse.current);
								pcDevicesPaired = 1;

							} else {

								players[1].InputContext.PerformPairingWithDevice(Mouse.current);
								pcDevicesPaired = 2;
							}
						} else {
							pcDevicesPaired = 1;
						}

					} else if (hasMouse) {
						players[0].InputContext.PerformPairingWithDevice(Mouse.current);

						pcDevicesPaired = 1;
					} else {
						pcDevicesPaired = 0;
					}

					for (int playerIndex = 0, nextGamepadIndex = 0; playerIndex < players.Count; ++playerIndex) {

						if (playerIndex < pcDevicesPaired)
							continue;

						if (nextGamepadIndex < gamepads.Count) {
							players[playerIndex].InputContext.PerformPairingWithDevice(gamepads[nextGamepadIndex]);
							nextGamepadIndex++;
						} else {
							players[playerIndex].InputContext.PerformPairingWithEmptyDevice();
						}
					}


				} else if (hasKeyboard && hasMouse) {

					var keyboards = InputSystem.devices.Where(d => d.enabled && d is Keyboard).ToList();
					var mice = InputSystem.devices.Where(d => d.enabled && d is Mouse).ToList();

					for (int i = 0; i < players.Count; i++) {
						var devices = i % 2 == 0 ? keyboards : mice;
						InputDevice device = i / 2 < devices.Count ? devices[i / 2] : null;

						if (device != null) {
							players[i].InputContext.PerformPairingWithDevice(device);
						} else {
							players[i].InputContext.PerformPairingWithEmptyDevice();
						}
					}

				} else {

					// Do nothing - everybody uses all the devices, if any?
				}
			}
		}

		private void SystemPauseLevels()
		{
			m_PlaythroughData.PausePlayers(playerWithInputPreserved: null, this);

			foreach (var player in m_PlaythroughData.ActivePlayers) {
				var recordComponent = player.LevelController.GetComponent<LevelReplayRecorder>();
				if (recordComponent) {
					recordComponent.Recording.AddAndRun(ReplayActionType.Pause);
				}
			}

		}

		private void SystemResumeLevels()
		{
			m_PlaythroughData.ResumePlayers(this);
		}
	}
}