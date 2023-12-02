using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.Input.UIInputDisplay;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using System.Linq;

namespace DevLocker.GFrame.SampleGame.Game
{
	/// <summary>
	/// Used to start the game:
	/// - Create a LevelsManager
	/// - Prepare Input and UI systems.
	/// - Set the starting level supervisor
	///
	/// Should be able to start the game from any game scene correctly.
	/// </summary>
	public class SampleGameStarter : MonoBehaviour
	{
		public SampleGameContext GameContext;

		public GameObject GameInputPrefab;
		public UIUtils.UISimpleCanvasGroupFader_LoadingScreen LevelFader;
		public MessageBox.MessageBox MessageBoxPrefab;

		public InputBindingDisplayAsset[] BindingDisplayAssets;

		// private as we don't want people accessing this singleton.
		private static SampleGameStarter m_Instance;

		private InputActionsMaskedStack.InputActionConflictsReport m_LastInputConflictsReport = new ();

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
			var levelFader = Instantiate(LevelFader.gameObject, transform).GetComponent<UIUtils.UISimpleCanvasGroupFader_LoadingScreen>();
			Instantiate(MessageBoxPrefab.gameObject, transform);

			gameInputObject.name = gameInputObject.name.Replace("(Clone)", "-Global");
			var playerInput = gameInputObject.GetComponentInChildren<PlayerInput>();
			playerInput.actions = playerControls.asset;

			var uiInputModule = gameInputObject.GetComponentInChildren<InputSystemUIInputModule>();
			uiInputModule.actionsAsset = playerControls.asset;  // This will refresh the UI Input action references to the new asset.

			playerInput.uiInputModule = uiInputModule;

			var inputContext = new InputComponentContext(playerInput, new InputActionsMaskedStack(playerControls), BindingDisplayAssets);
			playerControls.SetInputContext(inputContext);

			PlayerContextUIRootObject.GlobalPlayerContext.SetupGlobal(uiInputModule.GetComponent<EventSystem>(), inputContext);


			GameContext = new SampleGameContext(playerInput, playerControls, inputContext);

			var levelsManager = gameObject.AddComponent<SampleLevelsManager>();
			levelsManager.LevelLoadingScreen = levelFader;
			levelsManager.SetGameContext(GameContext);
		}

		void Start()
		{
			// Boot game from current scene
			if (GameObject.FindObjectOfType<PlayerInputManager>()) {
#if GFRAME_ASYNC
				SampleLevelsManager.Instance.SwitchLevelAsync(new Play.SampleMultiPlaySupervisor());
#else
				SampleLevelsManager.Instance.SwitchLevel(new Play.SampleMultiPlaySupervisor());
#endif
				return;
			}

			if (GameObject.FindObjectOfType<Play.SamplePlayerController>()) {
#if GFRAME_ASYNC
				SampleLevelsManager.Instance.SwitchLevelAsync(new Play.SamplePlaySupervisor());
#else
				SampleLevelsManager.Instance.SwitchLevel(new Play.SamplePlaySupervisor());
#endif
				return;
			}

			if (GameObject.FindObjectOfType<MainMenu.SampleMainMenuController>()) {
#if GFRAME_ASYNC
				SampleLevelsManager.Instance.SwitchLevelAsync(new MainMenu.SampleMainMenuLevelSupervisor());
#else
				SampleLevelsManager.Instance.SwitchLevel(new MainMenu.SampleMainMenuLevelSupervisor());
#endif
				return;
			}

			if (GameObject.FindObjectOfType<UITester.SampleUITesterController>()) {
#if GFRAME_ASYNC
				SampleLevelsManager.Instance.SwitchLevelAsync(new UITester.SampleUITesterLevelSupervisor());
#else
				SampleLevelsManager.Instance.SwitchLevel(new UITester.SampleUITesterLevelSupervisor());
#endif
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

#if UNITY_EDITOR
		private void Update()
		{
			if (Keyboard.current.f4Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ShowProcessing(
					"Level is downloading?", "",
					"The level is downloading. Please wait or go play another level.",
					MessageBoxIcon.Information,
					MessageBoxButtons.RetryCancel,
					new Dictionary<MessageBoxButtons, string>() { { MessageBoxButtons.Retry, "Start Level" } },
					new FakeProcessingProgressTracker(20),
					(res) => { Debug.Log($"Processing response - {res.MessageResponse}", this); },
					this
					);
				//Serialize();
			}

			if (Keyboard.current.f5Key.wasPressedThisFrame) {
				MessageBox.MessageBox.Instance.ShowInput(
					"Save?",
					"Are you sure you want to save?",
					"Savegame-001",
					null,
					MessageBoxIcon.Question,
					MessageBoxButtons.YesNo,
					(res) => { Debug.Log($"Save response - {res.MessageResponse}", this); },
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
					(res) => { Debug.Log($"Load response - {res.MessageResponse}", this); },
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