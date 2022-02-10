using DevLocker.GFrame;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerLostState : ILevelState, PlayerControls.ICommonHotkeysActions
	{
		private PlayerControls m_PlayerControls;
		private UI.TowerLevelUIController m_UIController;
		private ConeVisualsGrid m_VisualsGrid;
		private TowerConeVisualsController m_VisualsController;

		private ILostAnimationExecutor m_CurrentAnimation;
		private bool m_Interrupted;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_VisualsGrid);
			contextReferences.SetByType(out m_VisualsController);


			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.CommonHotkeys.SetCallbacks(this);
			m_PlayerControls.CommonHotkeys.Enable();
			// HACK: Listen for touch-screen tapping as they don't have CommonHotkeys (back button is not ideal).
			m_PlayerControls.TowerLevelPlay.PointerPress.performed += OnPointerPressed;
			m_PlayerControls.TowerLevelPlay.PointerPress.Enable();

			m_UIController.SwitchState(UI.TowerLevelUIState.Play);

			var lostAnimations = contextReferences.TryFindByType<ILostAnimationExecutor[]>();
			if (lostAnimations == null || lostAnimations.Length == 0) {
				GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
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

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.TowerLevelPlay.PointerPress.performed -= OnPointerPressed;
			m_PlayerControls.CommonHotkeys.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}

			return Task.CompletedTask;
		}

		private IEnumerator LostAnimationCoroutine()
		{
			var visualBlocks = new List<KeyValuePair<GridCoords, ConeVisualsBlock>>();

			for(int row = 0; row < m_VisualsGrid.Rows; ++row) {
				for(int column = 0; column < m_VisualsGrid.Columns; ++column) {
					ConeVisualsBlock visualBlock = m_VisualsGrid[row, column];
					if (visualBlock) {
						visualBlock.ClearHighlight();
						visualBlocks.Add(new KeyValuePair<GridCoords, ConeVisualsBlock>(new GridCoords(row, column), visualBlock));
					}
				}
			}

			Debug.Log($"Starting {m_CurrentAnimation.GetType().Name} lost animation.", m_CurrentAnimation as Object);
			yield return m_CurrentAnimation.Execute(m_VisualsGrid, m_VisualsController.FallingVisualsContainer, visualBlocks);

			if (m_Interrupted)
				yield break;

			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		public void OnBack(InputAction.CallbackContext context)
		{
			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}

			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		public void OnConfirm(InputAction.CallbackContext context)
		{
			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}

			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		private void OnPointerPressed(InputAction.CallbackContext obj)
		{
			if (!m_Interrupted) {
				m_Interrupted = true;
				m_CurrentAnimation.Interrupt();
			}

			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		public void OnNextSection(InputAction.CallbackContext context)
		{
		}

		public void OnPrevSection(InputAction.CallbackContext context)
		{
		}
	}
}