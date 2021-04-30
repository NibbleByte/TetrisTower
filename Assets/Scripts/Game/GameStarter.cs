using System.Collections;
using TetrisTower.Core;
using TetrisTower.HomeScreen;
using TetrisTower.Input;
using TetrisTower.TowerLevels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace TetrisTower.Game
{
	public class GameStarter : MonoBehaviour
	{
		public GameConfig GameConfig;

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

			// Use PlayerInput to properly assign input to users. Not useful at the moment but may be for future references.
			var playerInput = gameInputObject.GetComponentInChildren<PlayerInput>();
			playerInput.defaultActionMap = playerControls.UI.Get().name;
			playerInput.actions = playerControls.asset;

			playerInput.uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();

			var scheduler = gameObject.AddComponent<CoroutineScheduler>();

			GameContext = new GameContext(GameConfig, playerControls, playerInput, scheduler);

			gameObject.AddComponent<LevelSupervisorsManager>();
		}

		void Start()
		{
			// Boot game from current scene
			if (GameObject.FindObjectOfType<TowerLevelController>()) {
				GameContext.SetCurrentPlaythrough(GameConfig.NewGameData);
				LevelSupervisorsManager.Instance.SwitchLevel(new TowerLevelSupervisor(GameContext));
				return;
			}

			var homescreenLevel = GameObject.FindGameObjectWithTag("HomescreenLevel");
			if (homescreenLevel) {
				GameContext.SetCurrentPlaythrough(null);
				LevelSupervisorsManager.Instance.SwitchLevel(new HomeScreenLevelSupervisor(GameContext));
				return;
			}
		}

		void OnEnable()
		{
			if (GameContext != null) {
				//PlayerControls.Enable();
				GameContext.PlayerInput.ActivateInput();
			}
		}

		private void OnDisable()
		{
			if (GameContext != null) {
				//PlayerControls.Disable();
				GameContext.PlayerInput.DeactivateInput();
			}
		}

		private void OnDestroy()
		{
			if (m_Instance == this) {
				m_Instance = null;
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
			m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(GameContext.CurrentPlaythrough, GameConfig.Converters);
			Debug.Log(m_DebugSave);
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave))
				return;

			var playthrough = Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(m_DebugSave, GameConfig.Converters);
			GameContext.SetCurrentPlaythrough(playthrough);
			GameContext.StartCoroutine(new TowerLevelSupervisor(GameContext).Load());
		}

		#endregion
	}
}