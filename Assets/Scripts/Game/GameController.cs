using TetrisTower.Input;
using TetrisTower.TowerLevels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.Game
{
	public class GameController : MonoBehaviour
	{
		public GameControllerConfig GameControllerConfig;

		public PlayerControls PlayerControls { get; private set; }
		public PlayerInput PlayerInput { get; private set; }

		public PlaythroughData CurrentPlaythrough { get; private set; }
		[SerializeReference] private PlaythroughData m_DebugPlaythroughData;

		public GameObject UI;

		public static GameController Instance { get; private set; }
		public static GameControllerConfig Config => Instance != null ? Instance.GameControllerConfig : null;

		public TowerLevelController TowerLevel { get; private set; }

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

		}

		void Start()
		{
			PlayerControls = new PlayerControls();

			var gameInputObject = Instantiate(Config.GameInputPrefab, transform);

			// Use PlayerInput to properly assign input to users. Not useful at the moment but may be for future references.
			PlayerInput = gameInputObject.GetComponentInChildren<PlayerInput>();
			PlayerInput.defaultActionMap = PlayerControls.UI.Get().name;
			PlayerInput.actions = PlayerControls.asset;

			PlayerInput.uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();

			UI.SetActive(true);

			BootGameFromCurrentScene();
		}

		void OnEnable()
		{
			//PlayerControls.Enable();
			PlayerControls.UI.ResumeLevel.performed += TrySwitchToLevel;
			PlayerInput.ActivateInput();
		}

		private void OnDisable()
		{
			//PlayerControls.Disable();
			PlayerInput.DeactivateInput();
			PlayerControls.UI.ResumeLevel.performed += TrySwitchToLevel;
		}

		private void BootGameFromCurrentScene()
		{
			var towerLevel = GameObject.FindGameObjectWithTag("TowerLevel");
			if (towerLevel) {
				InitializeLevel(towerLevel.GetComponent<TowerLevelController>(), Config.NewGameData);
			}
		}

		public void StartNewGame(PlaythroughData playthroughData)
		{
			// TODO: Start new game.
		}

		private void InitializeLevel(TowerLevelController towerLevel, PlaythroughData playthrough)
		{
			CurrentPlaythrough = m_DebugPlaythroughData = playthrough;

			TowerLevel = towerLevel;
			TowerLevel.Init(playthrough.TowerLevel);

			var levelInput = towerLevel.GetComponent<TowerLevelInputController>();

			levelInput.FallSpeedup = Config.FallSpeedup;
			levelInput.Init(TowerLevel, SwitchInputToUI);
			PlayerControls.LevelGame.SetCallbacks(levelInput);
			SwitchInputToLevelGame();
		}


		public void SwitchInputToLevelGame()
		{
			PlayerInput.currentActionMap = PlayerControls.LevelGame.Get();
			TowerLevel.ResumeLevel();
			UI.SetActive(false);
		}

		public void SwitchInputToUI()
		{
			PlayerInput.currentActionMap = PlayerControls.UI.Get();
			TowerLevel.PauseLevel();
			UI.SetActive(true);
		}

		private void TrySwitchToLevel(InputAction.CallbackContext obj)
		{
			if (TowerLevel) {
				SwitchInputToLevelGame();
			}
		}

		private void OnDestroy()
		{
			if (Instance == this) {
				PlayerControls.Dispose();
				Instance = null;
			}
		}

		#region Debug Stuff

		private void Update()
		{
			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				Serialize();
			}
			if (Keyboard.current.f6Key.wasPressedThisFrame) {
				Deserialize();
			}
		}

		string m_DebugSave;
		void Serialize()
		{
			m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(CurrentPlaythrough, Config.Converters);
			Debug.Log(m_DebugSave);
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave))
				return;

			var playthrough = m_DebugPlaythroughData = Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(m_DebugSave, Config.Converters);
			var towerLevel = FindObjectOfType<TowerLevelController>();
			if (towerLevel) {
				InitializeLevel(towerLevel, playthrough);
			}
		}

		#endregion
	}
}