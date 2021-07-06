using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.SampleGame.MainMenu;
using DevLocker.GFrame.UIInputDisplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DevLocker.GFrame.SampleGame.Game
{
	public class SampleGameStarter : MonoBehaviour
	{
		public SampleGameContext GameContext;

		public GameObject GameInputPrefab;
		public MessageBox.MessageBox MessageBoxPrefab;

		public InputBindingDisplayAsset[] BindingDisplayAssets;

		// private as we don't want people accessing this singleton.
		private static SampleGameStarter m_Instance;

		void Awake()
		{
			if (m_Instance) {
				GameObject.DestroyImmediate(gameObject);
				return;
			}

			m_Instance = this;
			DontDestroyOnLoad(gameObject);

			var playerControls = new SamplePlayerControls();

			var gameInputObject = Instantiate(GameInputPrefab, transform);
			Instantiate(MessageBoxPrefab.gameObject, transform);

			var playerInput = gameInputObject.GetComponentInChildren<PlayerInput>();
			playerInput.actions = playerControls.asset;

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;  // This will refresh the UI Input action references to the new asset.

			playerInput.uiInputModule = uiInputModule;

			GameContext = new SampleGameContext(playerInput, playerControls, BindingDisplayAssets);

			var supervisorManager = gameObject.AddComponent<LevelsManager>();
			supervisorManager.SetGameContext(GameContext);
		}

		void Start()
		{
			// Boot game from current scene
			//if (GameObject.FindObjectOfType<TowerLevelController>()) {
			//	GameContext.SetCurrentPlaythrough(GameConfig.NewGameData);
			//	LevelsManager.Instance.SwitchLevel(new TowerLevelSupervisor());
			//	return;
			//}

			if (GameObject.FindObjectOfType<SampleMainMenuController>()) {
				LevelsManager.Instance.SwitchLevel(new SampleMainMenuLevelSupervisor());
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

#if UNITY_EDITOR
		private void Update()
		{
			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ShowInput(
					"Save?",
					"Are you sure you want to save?",
					"Savegame-001",
					null,
					MessageBoxIcon.Question,
					MessageBoxButtons.YesNo,
					(res) => { Debug.Log($"Save response - {res.ConfirmResponse}", this); },
					this
					);
				//Serialize();
			}

			if (Keyboard.current.f6Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ShowSimple(
					"Load?",
					"Are you sure you want to load?\nAll current progress will be lost!",
					MessageBoxIcon.Warning,
					MessageBoxButtons.YesNo,
					(res) => { Debug.Log($"Load response - {res.ConfirmResponse}", this); },
					this
					);
			}

			if (Keyboard.current.f7Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ForceConfirmShownMessage();
			}

			if (Keyboard.current.f8Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ForceDenyShownMessage();
			}
		}
#endif
	}
}