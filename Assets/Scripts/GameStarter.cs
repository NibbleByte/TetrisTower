using DevLocker.GFrame.Input.UIScope;
using TetrisTower.Game;
using TetrisTower.HomeScreen;
using TetrisTower.Logic;
using TetrisTower.TowerLevels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.GameStarter
{
	public class GameStarter : MonoBehaviour
	{
		public GameConfig GameConfig;

		[Tooltip("If starting tower level directly, override starting playthrough with this one, if specified.")]
		public PlaythroughTemplate StartingPlaythroughTemplate;

		[Tooltip("Leave the starting seed to 0 for random seed every time.")]
		public int StartingRandomSeed = 0;

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

			UIPlayerRootObject.GlobalUIRootObject.SetupGlobal(uiInputModule.GetComponent<EventSystem>());

			GameContext = new GameContext(GameConfig, playerControls);

			var supervisorManager = gameObject.AddComponent<GameManager>();
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
			// Boot game from current scene
			var towerLevelController = GameObject.FindObjectOfType<GridLevelController>();
			if (towerLevelController || GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag)) {

				DevLocker.Utils.SceneReference overrideScene = null;

				if (StartingPlaythroughTemplate) {

					PlaythroughData playthroughData = StartingPlaythroughTemplate.GeneratePlaythroughData(GameConfig);
					string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

					if (playthroughData.TowerLevel != null) {
						playthroughData.TowerLevel.BackgroundScene.ScenePath = scenePath;

						playthroughData.SetupRandomGenerator(StartingRandomSeed, true);

						TowerLevelDebugAPI.__DebugInitialTowerLevel = Newtonsoft.Json.JsonConvert.SerializeObject(playthroughData.TowerLevel, GameConfig.Converters);

					} else {
						overrideScene = new DevLocker.Utils.SceneReference(scenePath);

						playthroughData.SetupRandomGenerator(StartingRandomSeed);
					}
					GameContext.SetCurrentPlaythrough(playthroughData);

				} else {
					GameContext.SetCurrentPlaythrough(GameConfig.NormalPlaythgrough);
				}

				GameManager.Instance.SwitchLevelAsync(new TowerLevelSupervisor(overrideScene));
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
				GameContext.Dispose();
				m_Instance = null;
			}
		}
	}
}