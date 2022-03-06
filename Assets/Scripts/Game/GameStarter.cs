using TetrisTower.HomeScreen;
using TetrisTower.Logic;
using TetrisTower.TowerLevels;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.Game
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

			GameContext = new GameContext(GameConfig, playerControls);

			var supervisorManager = gameObject.AddComponent<GameManager>();
			supervisorManager.SetGameContext(GameContext);

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			Application.targetFrameRate = 30;
#else
			Application.targetFrameRate = 60;
#endif
		}

		void Start()
		{
			// Boot game from current scene
			var towerLevelController = GameObject.FindObjectOfType<GridLevelController>();
			if (towerLevelController || GameObject.FindGameObjectWithTag(GameTags.TowerPlaceholderTag)) {

				if (StartingPlaythroughTemplate) {

					PlaythroughData playthroughData = StartingPlaythroughTemplate.GeneratePlaythroughData(GameConfig);
					string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

					if (playthroughData.TowerLevel != null) {
						playthroughData.TowerLevel.BackgroundScene.ScenePath = scenePath;

						if (StartingRandomSeed != 0) {
							playthroughData.RandomSeed = StartingRandomSeed;
						}

						playthroughData.CreateRandomGenerator();
						playthroughData.TowerLevel.RandomInitialLevelSeed = playthroughData.Random.Next();
						playthroughData.TowerLevel.Random = new System.Random(playthroughData.TowerLevel.RandomInitialLevelSeed);

						TowerLevelDebugAPI.__DebugInitialTowerLevel = Newtonsoft.Json.JsonConvert.SerializeObject(playthroughData.TowerLevel, GameConfig.Converters);

					} else {
						LevelParamData levelParam = playthroughData.Levels[playthroughData.CurrentLevelIndex];
						levelParam.BackgroundScene.ScenePath = scenePath;

						if (StartingRandomSeed != 0) {
							playthroughData.RandomSeed = StartingRandomSeed;
						}
					}
					GameContext.SetCurrentPlaythrough(playthroughData);

				} else {
					GameContext.SetCurrentPlaythrough(GameConfig.NormalPlaythgrough);
				}

				GameManager.Instance.SwitchLevelAsync(new TowerLevelSupervisor());
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