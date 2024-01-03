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
	public class TowerLostState : IPlayerState
	{
		private IPlaythroughData m_PlaythroughData;
		private IPlayerContext m_PlayerContext;
		private PlayerControls m_PlayerControls;
		private GridLevelController m_LevelController;
		private TowerLevelUIController m_UIController;
		private ConeVisualsGrid m_VisualsGrid;
		private TowerConeVisualsController m_VisualsController;

		private ILostAnimationExecutor m_CurrentAnimation;
		private bool m_Interrupted;

		private float m_StartTime;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_PlaythroughData);
			context.SetByType(out m_PlayerContext);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_UIController);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_VisualsGrid);
			context.SetByType(out m_VisualsController);

			// Only in single player loser can interrupt animation.
			if (m_PlaythroughData.IsSinglePlayer) {
				m_PlayerControls.Enable(this, m_PlayerControls.UI);

				m_PlayerControls.CommonHotkeys.Back.performed += OnInterruptAnimation;
				m_PlayerControls.CommonHotkeys.Confirm.performed += OnInterruptAnimation;
				m_PlayerControls.Enable(this, m_PlayerControls.CommonHotkeys.Back);
				m_PlayerControls.Enable(this, m_PlayerControls.CommonHotkeys.Confirm);


				// HACK: Listen for touch-screen tapping as they don't have CommonHotkeys (back button is not ideal).
				m_PlayerControls.TowerLevelPlay.PointerPress.performed += OnInterruptAnimation;
				m_PlayerControls.Enable(this, m_PlayerControls.TowerLevelPlay.PointerPress);
			}

			m_UIController.SwitchState(TowerLevelUIState.Play);
			m_UIController.SetIsLevelPlaying(m_LevelController.LevelData.IsPlaying);

			m_StartTime = Time.time;

			var lostAnimations = context.TryFindByType<ILostAnimationExecutor[]>();
			if (lostAnimations == null || lostAnimations.Length == 0) {
				m_PlayerContext.StatesStack.SetState(new TowerFinishedLevelState());
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
			m_PlayerControls.TowerLevelPlay.PointerPress.performed -= OnInterruptAnimation;
			m_PlayerControls.CommonHotkeys.Back.performed -= OnInterruptAnimation;
			m_PlayerControls.CommonHotkeys.Confirm.performed -= OnInterruptAnimation;

			m_PlayerControls.DisableAll(this);

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

			m_PlayerContext.StatesStack.SetState(new TowerFinishedLevelState());
		}

		private IEnumerator LostAnimationCoroutine()
		{
			var visualBlocks = new List<KeyValuePair<GridCoords, ConeVisualsBlock>>();

			for(int row = 0; row < m_VisualsGrid.Rows; ++row) {
				for(int column = 0; column < m_VisualsGrid.Columns; ++column) {
					ConeVisualsBlock visualBlock = m_VisualsGrid[row, column];
					if (visualBlock) {
						visualBlock.ClearAnyHighlights();
						visualBlocks.Add(KeyValuePair.Create(new GridCoords(row, column), visualBlock));
					}
				}
			}

			Debug.Log($"Starting {m_CurrentAnimation.GetType().Name} lost animation.", m_CurrentAnimation as Object);
			yield return m_CurrentAnimation.Execute(m_VisualsGrid, m_VisualsController.FallingVisualsContainer, visualBlocks);

			if (m_Interrupted)
				yield break;

			m_PlayerContext.StatesStack.SetState(new TowerFinishedLevelState());
		}

		public void OnInterruptAnimation(InputAction.CallbackContext context)
		{
			RequestFinishUpState();
		}
	}
}