using DevLocker.GFrame.Input;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerUI;
using TetrisTower.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLostState : IPlayerState, PlayerControls.ICommonHotkeysActions
	{
		private PlayerControls m_PlayerControls;
		private GridLevelController m_LevelController;
		private TowerLevelUIController m_UIController;
		private ConeVisualsGrid m_VisualsGrid;
		private TowerConeVisualsController m_VisualsController;

		private InputEnabler m_InputEnabler;

		private ILostAnimationExecutor m_CurrentAnimation;
		private bool m_Interrupted;

		private float m_StartTime;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_VisualsGrid);
			context.SetByType(out m_VisualsController);


			m_InputEnabler = new InputEnabler(this);
			m_InputEnabler.Enable(m_PlayerControls.UI);
			m_InputEnabler.Enable(m_PlayerControls.CommonHotkeys);
			m_PlayerControls.CommonHotkeys.SetCallbacks(this);
			// HACK: Listen for touch-screen tapping as they don't have CommonHotkeys (back button is not ideal).
			m_PlayerControls.TowerLevelPlay.PointerPress.performed += OnPointerPressed;
			m_InputEnabler.Enable(m_PlayerControls.TowerLevelPlay.PointerPress);

			m_UIController.SwitchState(TowerLevelUIState.Play);
			m_UIController.SetIsLevelPlaying(m_LevelController.LevelData.IsPlaying);

			m_StartTime = Time.time;

			var lostAnimations = context.TryFindByType<ILostAnimationExecutor[]>();
			if (lostAnimations == null || lostAnimations.Length == 0) {
				GameManager.Instance.PushGlobalState(new TowerFinishedLevelState());
			}

			int totalChance = lostAnimations.Sum(la => la.Chance);
			int selectedChance = Random.Range(0, totalChance);
			int currentChance = 0;
			foreach(ILostAnimationExecutor lostAnimation in lostAnimations) {
				currentChance += lostAnimation.Chance;

				if (selectedChance < currentChance) {
					m_CurrentAnimation = lostAnimation;
					((MonoBehaviour)lostAnimation).StartCoroutine(LostAnimationCoroutine());
					break;
				}
			}
		}

		public void ExitState()
		{
			m_PlayerControls.TowerLevelPlay.PointerPress.performed -= OnPointerPressed;
			m_PlayerControls.CommonHotkeys.SetCallbacks(null);
			m_InputEnabler.Dispose();

			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}
		}

		private void RequestFinishUpState()
		{
			if (Time.time - m_StartTime < 2f)
				return;


			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}

			GameManager.Instance.PushGlobalState(new TowerFinishedLevelState());
		}

		private IEnumerator LostAnimationCoroutine()
		{
			var visualBlocks = new List<KeyValuePair<GridCoords, ConeVisualsBlock>>();

			for(int row = 0; row < m_VisualsGrid.Rows; ++row) {
				for(int column = 0; column < m_VisualsGrid.Columns; ++column) {
					ConeVisualsBlock visualBlock = m_VisualsGrid[row, column];
					if (visualBlock) {
						visualBlock.RestoreToNormal();
						visualBlocks.Add(new KeyValuePair<GridCoords, ConeVisualsBlock>(new GridCoords(row, column), visualBlock));
					}
				}
			}

			Debug.Log($"Starting {m_CurrentAnimation.GetType().Name} lost animation.", m_CurrentAnimation as Object);
			yield return m_CurrentAnimation.Execute(m_VisualsGrid, m_VisualsController.FallingVisualsContainer, visualBlocks);

			if (m_Interrupted)
				yield break;

			GameManager.Instance.PushGlobalState(new TowerFinishedLevelState());
		}

		public void OnBack(InputAction.CallbackContext context)
		{
			RequestFinishUpState();
		}

		public void OnConfirm(InputAction.CallbackContext context)
		{
			RequestFinishUpState();
		}

		private void OnPointerPressed(InputAction.CallbackContext obj)
		{
			RequestFinishUpState();
		}

		public void OnNextSection(InputAction.CallbackContext context)
		{
		}

		public void OnPrevSection(InputAction.CallbackContext context)
		{
		}
	}
}