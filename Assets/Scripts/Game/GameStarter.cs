using DevLocker.GFrame;
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

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;

			uiInputModule.point = InputActionReference.Create(playerControls.UI.Point);
			uiInputModule.leftClick = InputActionReference.Create(playerControls.UI.Click);
			uiInputModule.middleClick = InputActionReference.Create(playerControls.UI.MiddleClick);
			uiInputModule.rightClick = InputActionReference.Create(playerControls.UI.RightClick);
			uiInputModule.scrollWheel = InputActionReference.Create(playerControls.UI.ScrollWheel);
			uiInputModule.move = InputActionReference.Create(playerControls.UI.Navigate);
			uiInputModule.submit = InputActionReference.Create(playerControls.UI.Submit);
			uiInputModule.cancel = InputActionReference.Create(playerControls.UI.Cancel);
			uiInputModule.trackedDevicePosition = InputActionReference.Create(playerControls.UI.TrackedDevicePosition);
			uiInputModule.trackedDeviceOrientation = InputActionReference.Create(playerControls.UI.TrackedDeviceOrientation);

			// Make sure no input is enabled when starting level (including UI).
			playerControls.Disable();

			var scheduler = gameObject.AddComponent<CoroutineScheduler>();

			GameContext = new GameContext(GameConfig, playerControls, scheduler);

			var supervisorManager = gameObject.AddComponent<LevelsManager>();
			supervisorManager.SetGameContext(GameContext);
		}

		void Start()
		{
			// Boot game from current scene
			if (GameObject.FindObjectOfType<TowerLevelController>()) {
				GameContext.SetCurrentPlaythrough(GameConfig.NewGameData);
				LevelsManager.Instance.SwitchLevel(new TowerLevelSupervisor());
				return;
			}

			if (GameObject.FindObjectOfType<HomeScreenController>()) {
				GameContext.SetCurrentPlaythrough(null);
				LevelsManager.Instance.SwitchLevel(new HomeScreenLevelSupervisor());
				return;
			}
		}

		private void OnDestroy()
		{
			if (m_Instance == this) {
				m_Instance = null;
			}
		}

		#region Debug Stuff

#if UNITY_EDITOR
		private void Update()
		{
			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				Serialize();
			}
			if (Keyboard.current.f6Key.wasPressedThisFrame) {
				Deserialize();
			}

			if (Keyboard.current.f4Key.wasPressedThisFrame) {
				if (!GameContext.Controls.devices.HasValue) {
					Debug.LogWarning("Forcing pointer exclusive input!");
					GameContext.Controls.devices = new InputDevice[] { (InputDevice)Touchscreen.current ?? Mouse.current};
				} else {
					Debug.LogWarning("All devices are processed.");
					GameContext.Controls.devices = default;
				}
			}
		}
#endif

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
			LevelsManager.Instance.SwitchLevel(new TowerLevelSupervisor());
		}

		#endregion
	}
}