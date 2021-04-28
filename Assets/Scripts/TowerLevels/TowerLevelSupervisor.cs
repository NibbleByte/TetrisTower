using System;
using System.Collections;
using TetrisTower.Core;
using TetrisTower.Game;
using TetrisTower.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TetrisTower.TowerLevels
{
	public class TowerLevelSupervisor : ILevelSupervisor, IGameContextProvider
	{
		public GameContext GameContext { get; private set; }
		private PlayerControls m_PlayerControls => GameContext.PlayerControls;
		private PlayerInput m_PlayerInput => GameContext.PlayerInput;

		private TowerLevelController m_TowerLevelController;


		public TowerLevelSupervisor(GameContext gameContext)
		{
			GameContext = gameContext;
		}

		public IEnumerator Load()
		{
			if (SceneManager.GetActiveScene().name != "GameScene") {
				yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
			}

			var towerLevel = GameObject.FindGameObjectWithTag("TowerLevel");
			if (towerLevel == null) {
				throw new Exception("Couldn't find level in the scene.");
			}

			m_TowerLevelController = towerLevel.GetComponent<TowerLevelController>();
			m_TowerLevelController.Init(GameContext.CurrentPlaythrough.TowerLevel);

			var levelInput = towerLevel.GetComponent<TowerLevelInputController>();

			levelInput.FallSpeedup = GameContext.GameConfig.FallSpeedup;
			levelInput.Init(m_TowerLevelController, SwitchInputToUI);
			m_PlayerControls.LevelGame.SetCallbacks(levelInput);

			m_PlayerControls.UI.ResumeLevel.performed += OnResumeInputRequest;

			SwitchInputToLevelGame();
		}

		public IEnumerator Unload()
		{
			m_PlayerControls.UI.ResumeLevel.performed -= OnResumeInputRequest;

			yield break;
		}


		public void SwitchInputToLevelGame()
		{
			m_PlayerInput.currentActionMap = m_PlayerControls.LevelGame.Get();
			m_TowerLevelController.ResumeLevel();
			//UI.SetActive(false);
		}

		public void SwitchInputToUI()
		{
			m_PlayerInput.currentActionMap = m_PlayerControls.UI.Get();
			m_TowerLevelController.PauseLevel();
			//UI.SetActive(true);
		}

		private void OnResumeInputRequest(InputAction.CallbackContext obj)
		{
			SwitchInputToLevelGame();
		}
	}
}