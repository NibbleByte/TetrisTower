using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.MessageBox;
using System;
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
		private int m_PlayersCount;
		private Bounds m_FirstPlayerBounds;

		public void SetFirstPlayerBounds(Bounds bounds) => m_FirstPlayerBounds = bounds;

		public TowerLevelPlayersManager(IPlaythroughData playthroughData, int playersCount)
		{
			m_PlaythroughData = playthroughData;
			m_PlayersCount = playersCount;
		}

		public PlaythroughPlayer SetupPlayer(GameConfig config, int playerIndex, GridLevelController levelController, GridLevelData levelData, Camera camera, PlayerContextUIRootObject playerContextRoot, Canvas[] uiCanvases)
		{
			var playthroughPlayer = PlaythroughPlayer.Create(config, isMultiplayer: m_PlayersCount > 1, playerIndex, levelController, camera, playerContextRoot, uiCanvases);
			m_PlaythroughData.AssignPlayer(playthroughPlayer, levelData);

			SetupCamera(camera, playerIndex);

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

		private void SetupCamera(Camera camera, int playerIndex)
		{
			if (playerIndex != 0) {
				// Only one audio listener allowed.
				GameObject.DestroyImmediate(camera.GetComponentInChildren<AudioListener>(true));

			} else if (m_PlayersCount > 1) {
				// For split-screen, create an artifitial listener somewhere between the players,
				// so left player hears sounds from the left speaker, right one from the right speaker.
				// Also see TowerPlayerSound.
				GameObject.DestroyImmediate(camera.GetComponentInChildren<AudioListener>(true));

				// This should be synched with SetupOffset().
				// This supports for up to 4 players.
				Vector3 listenerPos;
				listenerPos.x = m_FirstPlayerBounds.max.x;
				listenerPos.y = camera.transform.position.y;
				listenerPos.z = m_PlayersCount == 2 ? m_FirstPlayerBounds.center.z : m_FirstPlayerBounds.min.z;

				var audioListener = new GameObject("AudioListener").AddComponent<AudioListener>();
				audioListener.transform.position = listenerPos;
			}

			switch (m_PlayersCount) {
				// Full screen
				case 1:
					camera.rect = new Rect(0f, 0f, 1f, 1f);
					break;

				// Side by side
				case 2:
				case 3:
					camera.rect = new Rect(playerIndex * (1f / m_PlayersCount), 0f, 1f / m_PlayersCount, 1f);
					camera.fieldOfView += m_PlayersCount == 2 ? 5 : 10;
					break;

				// 4 corners
				case 4:
					camera.rect = new Rect((playerIndex % 2) * (2f / m_PlayersCount), ((3 - playerIndex) / 2) * (2f / m_PlayersCount), 2f / m_PlayersCount, 2f / m_PlayersCount);
					break;

				default:
					throw new NotSupportedException($"{m_PlayersCount} players not supported.");
			}
		}

		public void SetupOffset(int playerIndex, GameObject[] sceneRoots)
		{
			if (playerIndex == 0)
				return;

			// Translate each player objects so they don't collide. First player remains in place.
			foreach (GameObject root in sceneRoots) {
				// NOTE: the positioning matters for the audio-listener that will be placed in the center between the players.
				//		 Check SetupCamera()

				// Since 3 players are displayed side by side in 3 columns, we do a special case so the audio makes some sense:
				// 1st player on the left, 3rd player on the right, 2nd player in the middle back.
				if (m_PlayersCount == 3) {
					if (playerIndex == 1) {
						// 2nd player
						root.transform.position += Vector3.right * (m_FirstPlayerBounds.center.x + m_FirstPlayerBounds.extents.x)
													- Vector3.forward * (m_FirstPlayerBounds.center.z + m_FirstPlayerBounds.size.z);
					} else {
						// 3rd player
						root.transform.position += Vector3.right * (m_FirstPlayerBounds.center.x + m_FirstPlayerBounds.size.x);
					}

				} else {
					root.transform.position += Vector3.right * (playerIndex % 2) * (m_FirstPlayerBounds.center.x + m_FirstPlayerBounds.size.x)
												- Vector3.forward * (playerIndex / 2) * (m_FirstPlayerBounds.size.z + m_FirstPlayerBounds.center.z);
				}

			}
		}

		public void SetupPlayersInputForReplay()
		{
			MessageBox.Instance.MessageShown += SystemPauseLevels;
			MessageBox.Instance.MessageClosed += SystemResumeLevels;

			var players = m_PlaythroughData.ActivePlayers.ToList();

			// First player has all the control, the rest have none.
			for(int playerIndex = 1; playerIndex < players.Count; playerIndex++) {
				players[playerIndex].InputContext.PerformPairingWithEmptyDevice();
			}
		}

		public void SetupPlayersInputForPVP()
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
					recordComponent.PlayerRecording.AddAndRun(ReplayActionType.Pause);
				}
			}

		}

		private void SystemResumeLevels()
		{
			m_PlaythroughData.ResumePlayers(this);
		}
	}
}