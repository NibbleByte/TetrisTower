using TetrisTower.Core;
using TetrisTower.Input;
using TetrisTower.TowerLevels;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.Game
{
	public class GameController : MonoBehaviour
	{
		public PlayerControls PlayerControls { get; private set; }
		public PlayerInput PlayerInput { get; private set; }

		public AssetsRepository AssetsRepository;
		public TowerLevelInputController LevelInputController;

		public GameObject UI;

		public TowerLevelData NewGameData;

		public static GameController Instance { get; private set; }

		public TowerLevelController TowerLevel { get; private set; }

		public Newtonsoft.Json.JsonConverter[] Converters { get; private set; }

		void Awake()
		{
			if (Instance) {
				GameObject.DestroyImmediate(gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(gameObject);

			Converters = new Newtonsoft.Json.JsonConverter[] {
				new BlockTypeConverter(AssetsRepository),
				new GridShapeTemplateConverter(AssetsRepository),
			};

			PlayerControls = new PlayerControls();

			// Use PlayerInput to properly assign input to users. Not useful at the moment but may be for future references.
			PlayerInput = GetComponent<PlayerInput>();
			PlayerInput.defaultActionMap = PlayerControls.UI.Get().name;
			PlayerInput.actions = PlayerControls.asset;

			PlayerInput.uiInputModule = FindObjectOfType<InputSystemUIInputModule>();

			UI.SetActive(true);
		}

		void Start()
		{
			// For Debug
			var towerLevel = FindObjectOfType<TowerLevelController>();
			if (towerLevel) {
				InitializeLevel(towerLevel, NewGameData);
			}
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

		public void StartNewGame(TowerLevelData levelData)
		{
			// TODO: Start new game.
		}

		private void InitializeLevel(TowerLevelController towerLevel, TowerLevelData data)
		{
			TowerLevel = towerLevel;
			TowerLevel.Init(data);

			var levelInput = towerLevel.GetComponent<TowerLevelInputController>();

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
			m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(TowerLevel.LevelData, Converters);
			Debug.Log(m_DebugSave);
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave))
				return;

			NewGameData = Newtonsoft.Json.JsonConvert.DeserializeObject<TowerLevelData>(m_DebugSave, Converters);
			Start();
		}

		#endregion
	}
}