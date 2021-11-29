using DevLocker.GFrame;
using DevLocker.GFrame.MessageBox;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.HomeScreen;
using TetrisTower.Logic;
using TetrisTower.TowerLevels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.Game
{
	public class GameStarter : MonoBehaviour
	{
		public GameConfig GameConfig;

		[Tooltip("If starting tower level directly, override starting playthrough with this one, if specified.")]
		public PlaythroughTemplate StartingPlaythroughTemplate;

		[SerializeReference]
		public GameContext GameContext;

		// private as we don't want people accessing this singleton.
		private static GameStarter m_Instance;

		void Awake()
		{
			if (m_Instance) {
				GameObject.DestroyImmediate(gameObject);
				return;
			}

			m_Instance = this;
			DontDestroyOnLoad(gameObject);

			var playerControls = new PlayerControls();

			var gameInputObject = Instantiate(GameConfig.GameInputPrefab, transform);
			Instantiate(GameConfig.MessageBoxPrefab.gameObject, transform);

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;

			var scheduler = gameObject.AddComponent<CoroutineScheduler>();

			GameContext = new GameContext(GameConfig, playerControls, scheduler);

			var supervisorManager = gameObject.AddComponent<GameManager>();
			supervisorManager.SetGameContext(GameContext);
		}

		void Start()
		{
			// Boot game from current scene
			var towerLevelController = GameObject.FindObjectOfType<GridLevelController>();
			if (towerLevelController || GameObject.FindGameObjectWithTag(GameConfig.TowerPlaceholderTag)) {

				if (StartingPlaythroughTemplate) {

					PlaythroughData playthroughData = StartingPlaythroughTemplate.GeneratePlaythroughData(GameConfig);
					string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
					if (playthroughData.TowerLevel != null) {
						playthroughData.TowerLevel.BackgroundScene.ScenePath = scenePath;
					} else {
						playthroughData.Levels[playthroughData.CurrentLevelIndex].BackgroundScene.ScenePath = scenePath;
					}
					GameContext.SetCurrentPlaythrough(playthroughData);
				} else {
					GameContext.SetCurrentPlaythrough(GameConfig.NormalPlaythgrough);
				}

				GameManager.Instance.SwitchLevel(new TowerLevelSupervisor());
				return;
			}

			if (GameObject.FindObjectOfType<HomeScreenController>()) {
				GameContext.ClearCurrentPlaythrough();
				GameManager.Instance.SwitchLevel(new HomeScreenLevelSupervisor());
				return;
			}
		}

		private void OnDestroy()
		{
			if (m_Instance == this) {
				GameContext.Dispose();
				m_Instance = null;
			}
		}

		#region Debug Stuff


#if UNITY_EDITOR
		private void Update()
		{
			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				MessageBox.Instance.ShowInput(
					"Save?",
					"Are you sure you want to save?",
					"Savegame-001",
					null,
					MessageBoxIcon.Question,
					MessageBoxButtons.YesNo,
					(res) => { if (res.ConfirmResponse) Serialize(); },
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
					Deserialize,
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
				if (!GameContext.PlayerControls.devices.HasValue) {
					Debug.LogWarning("Forcing pointer exclusive input!");
					GameContext.PlayerControls.devices = new InputDevice[] { (InputDevice)Touchscreen.current ?? Mouse.current};
				} else {
					Debug.LogWarning("All devices are processed.");
					GameContext.PlayerControls.devices = default;
				}
			}



			if (Keyboard.current.pKey.wasPressedThisFrame) {
				GameObject.FindObjectOfType<GridLevelController>().__DEBUG_ToggleFalling();
			}
		}

		string m_DebugSave;
		void Serialize()
		{

			if (GameContext.CurrentPlaythrough != null) {
				m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(GameContext.CurrentPlaythrough, GameConfig.Converters);
				Debug.Log(m_DebugSave);
			} else {
				Debug.Log("No game in progress.");
			}
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave)) {
				Debug.Log("No save found.");
				return;
			}

			var playthrough = Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(m_DebugSave, GameConfig.Converters);
			GameContext.SetCurrentPlaythrough(playthrough);
			GameManager.Instance.SwitchLevel(new TowerLevelSupervisor());
		}
#endif

		#endregion
	}
}