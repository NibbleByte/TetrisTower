using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System.Collections.Generic;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.Tools;
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

		private float m_FallSpeedOriginal;
		private GridLevelController m_TowerLevel;

		private GameContext m_Context;
		private UI.FlashMessageUIController m_FlashMessage;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStaticsCache()
		{
			__DebugInitialTowerLevel = string.Empty;
		}

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);
			contextReferences.SetByType(out m_Context);
			contextReferences.SetByType(out m_FlashMessage);
		}

		public void OnLevelUnloading()
		{
		}

		public void ToggleFalling()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Toggle Falling");

			if (m_TowerLevel.LevelData.FallSpeedNormalized == 0f) {
				m_TowerLevel.LevelData.FallSpeedNormalized = m_FallSpeedOriginal;
			} else {
				m_FallSpeedOriginal = m_TowerLevel.LevelData.FallSpeedNormalized;
				m_TowerLevel.LevelData.FallSpeedNormalized = 0f;
			}
		}

		public void ResetLevel()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Retry Level");

			UI.TowerLevelUIController.RetryLevel();
		}

		public void Win()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Win Level");

			m_TowerLevel.FinishLevel(TowerLevelRunningState.Won);
		}

		public void Lose()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Lose Level");

			m_TowerLevel.FinishLevel(TowerLevelRunningState.Lost);
		}

		public void Save()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Game Saved");

			SaveGame(m_Context);
		}

		public void Load()
		{
			if (m_FlashMessage) m_FlashMessage.AppendMessage("Game Loaded");

			LoadGame(m_Context);
		}

		public static void SaveGame(GameContext context)
		{
			if (context.CurrentPlaythrough != null) {
				string savedData = Newtonsoft.Json.JsonConvert.SerializeObject(context.CurrentPlaythrough, context.GameConfig.Converters);
				PlayerPrefs.SetString("Debug_SavedGame", savedData);
				PlayerPrefs.Save();
				Debug.Log(savedData);
			} else {
				Debug.Log("No game in progress.");
			}
		}

		public static void LoadGame(GameContext context)
		{
			string savedData = PlayerPrefs.GetString("Debug_SavedGame");
			if (string.IsNullOrEmpty(savedData)) {
				Debug.Log("No save found.");
				return;
			}

			var playthrough = Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(savedData, context.GameConfig.Converters);
			context.SetCurrentPlaythrough(playthrough);
			GameManager.Instance.SwitchLevelAsync(new TowerLevelSupervisor());
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
				if (!m_Context.PlayerControls.devices.HasValue) {
					Debug.LogWarning("Forcing pointer exclusive input!");
					m_Context.PlayerControls.devices = new InputDevice[] { (InputDevice)Touchscreen.current ?? Mouse.current };
				} else {
					Debug.LogWarning("All devices are processed.");
					m_Context.PlayerControls.devices = default;
				}
			}

			DebugClearRow();
		}

		private void DebugClearRow()
		{
#if UNITY_EDITOR
			if (Keyboard.current == null)
				return;

			// Temporary disable.
			for (Key key = Key.Digit1; key <= Key.Digit0; ++key) {

				int keyRow = (key - Key.Digit1 + 1) % 10;

				if (Keyboard.current[key].wasPressedThisFrame && keyRow < m_TowerLevel.Grid.Rows) {

					List<GridCoords> clearCoords = new List<GridCoords>();
					for (int column = 0; column < m_TowerLevel.Grid.Columns; ++column) {
						var coords = new GridCoords(keyRow, column);
						if (m_TowerLevel.Grid[coords]) {
							clearCoords.Add(coords);
						}
					}

					if (clearCoords.Count > 0) {
						var actions = new List<GridAction>() { new ClearMatchedAction() { MatchedType = MatchScoringType.Horizontal, Coords = clearCoords } };
						StartCoroutine(m_TowerLevel.RunActions(actions));
					}
				}
			}
#endif
		}
	}
}