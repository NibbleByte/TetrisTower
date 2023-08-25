using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.UIUtils;
using Newtonsoft.Json;
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
			var loadingScreen = Instantiate(GameConfig.LoadingScreenPrefab, transform).GetComponentInChildren<UISimpleCanvasGroupFader>(true);

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;

			var inputContext = new InputCollectionContext(playerControls, playerControls.UI.Get(), GameConfig.BindingDisplayAssets);
			playerControls.SetInputContext(inputContext);

			PlayerContextUIRootObject.GlobalPlayerContext.SetupGlobal(uiInputModule.GetComponent<EventSystem>(), inputContext);

			GameContext = new GameContext(GameConfig, playerControls, inputContext);

			var supervisorManager = gameObject.AddComponent<GameManager>();
			supervisorManager.LevelLoadingScreen = loadingScreen;
			supervisorManager.SetGameContext(GameContext);


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

		void Start()
		{
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

					if (playthroughData.TowerLevel != null) {
						playthroughData.TowerLevel.BackgroundScene.ScenePath = scenePath;

						playthroughData.SetupRandomGenerator(StartingRandomSeed, true);

						// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
						// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
						TowerLevelDebugAPI.__DebugInitialTowerLevel = Newtonsoft.Json.JsonConvert.SerializeObject(playthroughData.TowerLevel, new Newtonsoft.Json.JsonSerializerSettings() {
							Converters = Saves.SaveManager.GetConverters(GameConfig),
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

				} if (supervisor is WorldMapLevelSupervisor worldSupervisor) {
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

namespace TetrisTower.Saves
{
	// TODO: Temporary class till we find better place for these.
	public static class SaveManager
	{
		public static JsonConverter[] GetConverters(GameConfig config) => new JsonConverter[] {
				new BlocksSkinSetConverter(config.AssetsRepository),
				new LevelParamAssetConverter(config.AssetsRepository),
				new WorldLevelsSetConverter(config.AssetsRepository),
				new GridShapeTemplateConverter(config.AssetsRepository),
				new Core.RandomXoShiRo128starstarJsonConverter(),
		};

		public static TReturn Clone<TReturn>(object data, GameConfig config)
		{
			var serialized = Serialize<TReturn>(data, config);

			// No need to have the json "TypeNameHandling = Auto" of the root object serialized, as we specify the type in the generics parameter.
			return Deserialize<TReturn>(serialized, config);
		}

		public static TReturn Clone<TData, TReturn>(object data, GameConfig config)
		{
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			var serialized = JsonConvert.SerializeObject(data, typeof(TData), new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
				//Formatting = Formatting.Indented,
			});

			return Deserialize<TReturn>(serialized, config);
		}

		public static string Serialize<TData>(object data, GameConfig config)
		{
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			return JsonConvert.SerializeObject(data, typeof(TData), new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
				//Formatting = Formatting.Indented,
			});
		}

		public static TReturn Deserialize<TReturn>(string serializeData, GameConfig config)
		{
			// No need to have the json "TypeNameHandling = Auto" of the root object serialized, as we specify the type in the generics parameter.
			return JsonConvert.DeserializeObject<TReturn>(serializeData, new JsonSerializerSettings() {
				Converters = GetConverters(config),
				TypeNameHandling = TypeNameHandling.Auto,
			});
		}
	}

}