using TetrisTower.Core;
using TetrisTower.Input;
using TetrisTower.Levels;
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
		public LevelInputController LevelInputController;

		public GameObject UI;

		// TODO: REMOVE
		public LevelData StartData;

		public static GameController Instance { get; private set; }

		public LevelController LevelController { get; private set; }

		public Newtonsoft.Json.JsonConverter[] Converters { get; private set; }

		void Awake()
		{
			if (Instance) {
				gameObject.SetActive(false);
			}

			Instance = this;

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
			var levelController = FindObjectOfType<LevelController>();
			if (levelController) {
				InitializeLevel(levelController, StartData);
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

		private void InitializeLevel(LevelController level, LevelData data)
		{
			if (LevelController) {
				PlayerControls.LevelGame.SetCallbacks(null);
				Destroy(LevelController.gameObject);
			}

			LevelController = level;
			LevelController.Init(data);

			var levelInput = level.GetComponent<LevelInputController>();

			levelInput.Init(LevelController, SwitchInputToUI);
			PlayerControls.LevelGame.SetCallbacks(levelInput);
			SwitchInputToLevelGame();
		}


		public void SwitchInputToLevelGame()
		{
			PlayerInput.currentActionMap = PlayerControls.LevelGame.Get();
			LevelController.ResumeLevel();
			UI.SetActive(false);
		}

		public void SwitchInputToUI()
		{
			PlayerInput.currentActionMap = PlayerControls.UI.Get();
			LevelController.PauseLevel();
			UI.SetActive(true);
		}

		private void TrySwitchToLevel(InputAction.CallbackContext obj)
		{
			if (LevelController) {
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
			m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(LevelController.LevelData, Converters);
			Debug.Log(m_DebugSave);
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave))
				return;

			StartData = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelData>(m_DebugSave, Converters);
			Start();
		}

		#endregion
	}
}