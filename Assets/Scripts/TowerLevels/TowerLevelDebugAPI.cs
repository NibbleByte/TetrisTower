using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.MessageBox;
using System.Collections.Generic;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.Tools;
using TetrisTower.TowerLevels.Replays;
using TetrisTower.TowerUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelDebugAPI : MonoBehaviour, ILevelLoadedListener
	{
		public static string __DebugInitialTowerLevel;

		public DebugInfoDisplay DebugDisplayInfo;
		public GameObject ProfilerStatsPrefab;

		private GameObject m_ProfilerStatsObject;

		private IPlaythroughData m_PlaythroughData;
		private PlayerControls m_PlayerControls;
		private GridLevelController m_TowerLevel;
		private TowerStatesAPI m_TowerLevelAPI;
		private ReplayRecording m_ReplayRecording;

		private GameContext m_Context;
		private FlashMessageUIController m_FlashMessage;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStaticsCache()
		{
			__DebugInitialTowerLevel = string.Empty;
		}

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_TowerLevel);
			context.SetByType(out m_Context);
			context.SetByType(out m_FlashMessage);
			context.SetByType(out m_TowerLevelAPI);
			context.TrySetByType(out m_ReplayRecording);
		}

		public void OnLevelUnloading()
		{
		}

		public void ToggleFalling()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Toggle Falling");

			m_TowerLevel.LevelData.FallFrozen = !m_TowerLevel.LevelData.FallFrozen;
		}

		public void ToggleMatching()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Toggle Matching");

			m_TowerLevel.Grid.MatchingFrozen = !m_TowerLevel.Grid.MatchingFrozen;
		}

		public void ResetLevel()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Retry Level");

			m_TowerLevelAPI.RetryLevel();
		}

		public void Win()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Win Level");

			if (m_ReplayRecording != null) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Cheat_EndLevel, 1);
			}
		}

		public void Lose()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Lose Level");

			if (m_ReplayRecording != null) {
				m_ReplayRecording.AddAndRun(ReplayActionType.Cheat_EndLevel, 0);
			}
		}

		public void Save()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Game Saved");

			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			string savedData = Saves.SavesManager.Serialize<IPlaythroughData>(m_PlaythroughData, m_Context.GameConfig);
			PlayerPrefs.SetString("Debug_SavedGame", savedData);
			PlayerPrefs.Save();
			Debug.Log(savedData);
		}

		public void Load()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Game Loaded");

			string savedData = PlayerPrefs.GetString("Debug_SavedGame");
			if (string.IsNullOrEmpty(savedData)) {
				Debug.Log("No save found.");
				return;
			}

			var playthrough = Saves.SavesManager.Deserialize<IPlaythroughData>(savedData, m_Context.GameConfig);

			m_Context.SetCurrentPlaythrough(playthrough);
			GameManager.Instance.SwitchLevelAsync(playthrough.PrepareSupervisor());
		}

		public void ToggleBirds()
		{
#if USE_BIRD_FLOCKS
			foreach (var birdController in GameObject.FindObjectsOfType<FlockController>(true)) {
				birdController.gameObject.SetActive(!birdController.gameObject.activeSelf);
			}

			foreach(var landingSpotController in GameObject.FindObjectsOfType<LandingSpotController>(true)) {
				landingSpotController.gameObject.SetActive(!landingSpotController.gameObject.activeSelf);
			}
#endif
		}

		public void ToggleDebug()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Debug Toggle");

			if (m_ProfilerStatsObject == null && ProfilerStatsPrefab) {
				m_ProfilerStatsObject = Instantiate(ProfilerStatsPrefab);
				return;
			}

			if (m_ProfilerStatsObject == null) {
				DebugDisplayInfo.gameObject.SetActive(!DebugDisplayInfo.gameObject.activeSelf);
				return;
			}

			if (!DebugDisplayInfo.gameObject.activeSelf && !m_ProfilerStatsObject.activeSelf) {
				m_ProfilerStatsObject.SetActive(true);

			} else if (m_ProfilerStatsObject.activeSelf) {
				m_ProfilerStatsObject.SetActive(false);
				DebugDisplayInfo.gameObject.SetActive(true);
			} else {
				DebugDisplayInfo.gameObject.SetActive(false);
			}

		}

		void Update()
		{
			if (Keyboard.current.fKey.wasPressedThisFrame) {
				ToggleFalling();
			}

			if (Keyboard.current.mKey.wasPressedThisFrame) {
				ToggleMatching();
			}

			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				MessageBox.Instance.ShowInput(
					"Save?",
					"Are you sure you want to save?",
					"Savegame-001",
					null,
					MessageBoxIcon.Question,
					MessageBoxButtons.YesNo,
					(res) => { if (res.ConfirmResponse) Save(); },
					this
					);
				//Serialize();
			}

			if (Keyboard.current.f6Key.wasPressedThisFrame) {
				MessageBox.Instance.ShowSimple(
					"Load?",
					"Are you sure you want to load?\nAll current progress will be lost!",
					MessageBoxIcon.Warning,
					MessageBoxButtons.YesNo,
					Load,
					this
					);

				//Deserialize();
			}

			if (Keyboard.current.f7Key.wasPressedThisFrame) {
				MessageBox.Instance.ForceConfirmShownMessage();
			}

			if (Keyboard.current.f8Key.wasPressedThisFrame) {
				MessageBox.Instance.ForceDenyShownMessage();
			}

			if (Keyboard.current.f4Key.wasPressedThisFrame) {
				if (!m_PlayerControls.devices.HasValue) {
					Debug.LogWarning("Forcing pointer exclusive input!");
					m_PlayerControls.devices = new InputDevice[] { (InputDevice)Touchscreen.current ?? Mouse.current };
				} else {
					Debug.LogWarning("All devices are processed.");
					m_PlayerControls.devices = default;
				}
			}

			DebugClearRow();
		}

		private void DebugClearRow()
		{
#if UNITY_EDITOR
			if (Keyboard.current == null || Keyboard.current.altKey.isPressed || Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed)
				return;

			// Temporary disable.
			for (Key key = Key.Digit1; key <= Key.Digit0; ++key) {

				int keyRow = (key - Key.Digit1 + 1) % 10;

				if (Keyboard.current[key].wasPressedThisFrame && keyRow < m_TowerLevel.Grid.Rows) {

					List<GridCoords> clearCoords = new List<GridCoords>();
					for (int column = 0; column < m_TowerLevel.Grid.Columns; ++column) {
						var coords = new GridCoords(keyRow, column);
						if (m_TowerLevel.Grid[coords] != BlockType.None) {
							clearCoords.Add(coords);
						}
					}

					if (clearCoords.Count > 0) {
						var actions = new List<GridAction>() { new ClearMatchedAction() { MatchedType = MatchScoringType.Horizontal, Coords = clearCoords } };
						m_TowerLevel.StartRunActions(actions);
					}
				}
			}
#endif
		}
	}
}