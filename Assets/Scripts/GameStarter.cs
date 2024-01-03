using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.UIUtils;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.HomeScreen;
using TetrisTower.Logic;
using TetrisTower.TowerLevels;
using TetrisTower.TowerLevels.Playthroughs;
using TetrisTower.WorldMap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.GameStarter
{
	public class GameStarter : MonoBehaviour
	{
		public GameConfig GameConfig;

		[Tooltip("If starting tower level directly, override starting playthrough with this one, if specified.")]
		public PlaythroughTemplateBase StartingPlaythroughTemplate;

		[Tooltip("Leave the starting seed to 0 for random seed every time.")]
		public int StartingRandomSeed = 0;

		[SerializeReference]
		public GameContext GameContext;

		// private as we don't want people accessing this singleton.
		private static GameStarter m_Instance;

		private DevLocker.GFrame.Input.InputActionsMaskedStack.InputActionConflictsReport m_LastInputConflictsReport = new();

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
			var loadingScreen = Instantiate(GameConfig.LoadingScreenPrefab, transform).GetComponentInChildren<UISimpleCanvasGroupFader_LoadingScreen>(true);
			var systemOverlay = Instantiate(GameConfig.SystemOverlayPrefab, transform);

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;

			var inputContext = new InputCollectionContext(playerControls, playerControls.UI.Get(), GameConfig.BindingDisplayAssets);
			playerControls.SetInputContext(inputContext);

			PlayerContextUIRootObject.GlobalPlayerContext.SetupGlobal(uiInputModule.GetComponent<EventSystem>(), inputContext);

			GameContext = new GameContext(GameConfig, playerControls, inputContext);

			var supervisorManager = gameObject.AddComponent<GameManager>();
			supervisorManager.LevelLoadingScreen = loadingScreen;
			supervisorManager.SetGameContext(GameContext);
			supervisorManager.SetManagers(
				systemOverlay.GetComponentInChildren<SystemUI.ToastNotificationsController>(),
				systemOverlay.GetComponentInChildren<SystemUI.BlockingOperationOverlayController>(true)
				);


			if (Platforms.PlatformsUtils.IsMobile) {
#if DEVELOPMENT_BUILD
				gameObject.AddComponent<Tools.DebugLogDisplay>();	// Because Android doesn't display dev console on errors. Have to use Logcat.
#endif

				Application.targetFrameRate = Screen.currentResolution.refreshRate;		// Max possible FPS.
				//Application.targetFrameRate = -1;										// Defaults to 30 FPS to save battery.
			} else {
				Application.targetFrameRate = 60;
			}
		}

		async void Start()
		{
			var loadedPrefs = await Saves.SavesManager.LoadPreferences(GameConfig);
			GameContext.EditPreferences().ApplyFrom(loadedPrefs);

			// Auto-save prefs every time they change. Find a better place for this someday?
			// If dragging sound bar too quickly will file writes fail? No as it runs on the main thread, no files writtern in parallel?
			GameContext.UserPrefs.Changed += () => Saves.SavesManager.SavePreferences(GameContext.EditPreferences(), GameConfig);

			if (GameObject.FindObjectOfType<WorldMapController>()) {
				var playthroughData = (WorldPlaythroughData) StartingPlaythroughTemplate.GeneratePlaythroughData(GameConfig);

				playthroughData.SetupRandomGenerator(StartingRandomSeed);

				GameContext.SetCurrentPlaythrough(playthroughData);

				var supervisor = GameContext.CurrentPlaythrough.PrepareSupervisor();

				GameManager.Instance.SwitchLevelAsync(supervisor);

				return;
			}

			// Boot game from current scene
			var towerLevelController = GameObject.FindObjectOfType<GridLevelController>();
			if (towerLevelController || GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag)) {

				DevLocker.Utils.SceneReference overrideScene = null;

				if (StartingPlaythroughTemplate) {

					IPlaythroughData playthroughData = StartingPlaythroughTemplate.GeneratePlaythroughData(GameConfig);
					string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

					if (playthroughData.ActiveTowerLevels.Any()) {
						playthroughData.ActiveTowerLevels[0].BackgroundScene.ScenePath = scenePath;

						playthroughData.SetupRandomGenerator(StartingRandomSeed, true);

						// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
						// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
						TowerLevelDebugAPI.__DebugInitialTowerLevel = Newtonsoft.Json.JsonConvert.SerializeObject(playthroughData.ActiveTowerLevels[0], new Newtonsoft.Json.JsonSerializerSettings() {
							Converters = Saves.SavesManager.GetConverters(GameConfig),
							TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
							//Formatting = Formatting.Indented,
						});

					} else {
						overrideScene = new DevLocker.Utils.SceneReference(scenePath);

						playthroughData.SetupRandomGenerator(StartingRandomSeed);
					}
					GameContext.SetCurrentPlaythrough(playthroughData);

				} else {
#if UNITY_EDITOR
					GameContext.SetCurrentPlaythrough(GameConfig.DevDefaultPlaythgrough);
#else
					throw new System.NotImplementedException();
#endif
				}

				var supervisor = GameContext.CurrentPlaythrough.PrepareSupervisor();
				if (supervisor is TowerLevelSupervisor seqSupervisor) {
					seqSupervisor.SetSceneOverride(overrideScene);

				} else if (supervisor is WorldMapLevelSupervisor worldSupervisor) {
					WorldPlaythroughData worldPlaythrough = (WorldPlaythroughData)GameContext.CurrentPlaythrough;

					string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
					WorldMapLevelParamData level = worldPlaythrough.GetAllLevels()
						.OfType<WorldMapLevelParamData>()
						.FirstOrDefault(l => l.BackgroundScene.ScenePath == scenePath || l.BackgroundSceneMobile.ScenePath == scenePath)
						;

					if (level != null) {
						worldPlaythrough.SetCurrentLevel(level.LevelID);
						supervisor = worldPlaythrough.PrepareSupervisor();	// Will change the supervisor type.
					}
				} else {
					Debug.LogWarning($"Unsupported supervisor {supervisor}", this);
				}
				GameManager.Instance.SwitchLevelAsync(supervisor);
				return;
			}

			if (GameObject.FindObjectOfType<HomeScreenController>()) {
				GameContext.ClearCurrentPlaythrough();
				GameManager.Instance.SwitchLevelAsync(new HomeScreenLevelSupervisor());
				return;
			}
		}

		private void OnDestroy()
		{
			if (m_Instance == this) {
				m_Instance = null;
			}
		}

		private void LateUpdate()
		{
			// Check for InputActions conflicts at the end of every frame and report.
			var inputContext = PlayerContextUIRootObject.GlobalPlayerContext.InputContext;
			if (inputContext != null) {
				var conflictsReport = inputContext.InputActionsMaskedStack.GetConflictingActionRequests(inputContext.GetUIActions());
				if (!m_LastInputConflictsReport.Equals(conflictsReport) && conflictsReport.HasIssuesFound) {
					var conflictStrings = conflictsReport.Conflicts.Select(pair => $"- {pair.Key.name} [{string.Join(", ", pair.Value)}]");
					var illegalStrings = conflictsReport.IllegalActions.Select(action => $"- {action.name} [ILLEGAL]");

					Debug.LogError($"[Input] Input actions in conflict found:\n{string.Join('\n', conflictStrings.Concat(illegalStrings))}", this);
				}

				m_LastInputConflictsReport = conflictsReport;
			}
		}
	}
}