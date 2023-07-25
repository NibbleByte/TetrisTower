using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.MessageBox;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.InputSystem;
using static TetrisTower.TowerLevels.Playthroughs.WorldPlaythroughData;

namespace TetrisTower.WorldMap
{
	public class WorldMapDebugAPI : MonoBehaviour, ILevelLoadedListener
	{
		private WorldMapController m_WorldLevel;
		private WorldPlaythroughData m_PlaythroughData;

		public GameObject ProfilerStatsPrefab;
		private GameObject m_ProfilerStatsObject;

		private GameContext m_Context;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_WorldLevel);
			context.SetByType(out m_Context);

			m_PlaythroughData = (WorldPlaythroughData)m_Context.CurrentPlaythrough;
		}

		public void OnLevelUnloading()
		{
		}

		public void ToggleDebug()
		{
			if (m_ProfilerStatsObject == null && ProfilerStatsPrefab) {
				m_ProfilerStatsObject = Instantiate(ProfilerStatsPrefab);
				return;
			}

			m_ProfilerStatsObject.SetActive(!m_ProfilerStatsObject.activeSelf);
		}

		public void UnlockLevels()
		{
			var accomplishments = new List<WorldLevelAccomplishment>();

			foreach(var levelData in m_PlaythroughData.GetAllLevels().OfType<WorldMapLevelParamData>()) {
				WorldLevelAccomplishment accomplishment = m_PlaythroughData.GetAccomplishment(levelData.LevelID);

				if (!accomplishment.IsValid) {
					accomplishment.LevelID = levelData.LevelID;
				}

				if (accomplishment.State == WorldLocationState.Hidden) {
					accomplishment.State = WorldLocationState.Unlocked;
				}

				accomplishments.Add(accomplishment);
			}

			m_PlaythroughData.__ReplaceAccomplishments(accomplishments);
			m_WorldLevel.StartCoroutine(m_WorldLevel.RevealUnlockedLocations());
		}

		public void Save()
		{
			SaveGame(m_Context);
		}

		public void Load()
		{
			LoadGame(m_Context);
		}

		public static void SaveGame(GameContext context)
		{
			if (context.CurrentPlaythrough != null) {
				// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
				string savedData = Saves.SaveManager.Serialize<IPlaythroughData>(context.CurrentPlaythrough, context.GameConfig);
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

			var playthrough = Saves.SaveManager.Deserialize<IPlaythroughData>(savedData, context.GameConfig);

			context.SetCurrentPlaythrough(playthrough);
			GameManager.Instance.SwitchLevelAsync(playthrough.PrepareSupervisor());
		}

		void Update()
		{
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
			}
		}

	}
}