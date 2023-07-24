using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System.Collections.Generic;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerWonState : IPlayerState, PlayerControls.ICommonHotkeysActions, IUpdateListener
	{
		private GameConfig m_GameConfig;
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;
		private GridLevelController m_LevelController;
		private GridLevelData m_LevelData => m_LevelController.LevelData;

		private FlashMessageUIController m_FlashMessage;

		private int m_BonusBlocksCount = 0;

		private float m_BonusFillUpTime = float.NaN;

		private float m_StartTime;

		private InputEnabler m_InputEnabler;

		public void EnterState(PlayerStatesContext context)
		{
			context.SetByType(out m_GameConfig);
			context.SetByType(out m_UIController);
			context.SetByType(out m_PlayerControls);
			context.SetByType(out m_LevelController);
			context.SetByType(out m_FlashMessage);


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

			if (m_LevelData.FallingShape != null) {
				// HACK: This happens only via cheats. This will eventually the currently displayed falling shape.
				m_LevelData.FallingShape = null;
				Update();
			}
		}

		public void ExitState()
		{
			m_PlayerControls.TowerLevelPlay.PointerPress.performed -= OnPointerPressed;
			m_PlayerControls.CommonHotkeys.SetCallbacks(null);
			m_InputEnabler.Dispose();

			m_FlashMessage.ClearMessage();
		}

		private void RequestFinishUpState()
		{
			if (Time.time - m_StartTime < 2f)
				return;

			InterruptAnimation();
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

		private void InterruptAnimation()
		{
			m_LevelData.Score.ConsumeBonusScore();
			m_LevelController.ClearSelectedShape();

			var shapeCoords = new List<BlocksShape.ShapeBind>();

			int rows = m_LevelData.PlayableSize.Row;

			for (int row = 0; row < rows; ++row) {
				for (int column = 0; column < m_LevelData.Grid.Columns; ++column) {
					if (m_LevelData.Grid[row, column] == BlockType.None) {
						shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
							Coords = new GridCoords(row, column),
							Value = BlockType.WonBonusBlock,
						});
					}
				}
			}

			var actions = new List<GridAction>() {
				new PlaceAction() { PlaceCoords = GridCoords.Zero, PlacedShape = new BlocksShape() { ShapeCoords = shapeCoords.ToArray() } }
			};

			m_LevelController.StartCoroutine(m_LevelController.RunActions(actions));
		}

		private bool IsFilledUpWithBonusBlocks()
		{
			for (int column = 0; column < m_LevelData.Grid.Columns; ++column) {
				if (m_LevelData.Grid[m_LevelData.PlayableSize.Row - 1, column] == BlockType.None)
					return false;
			}

			return true;
		}

		public void Update()
		{
			if (!float.IsNaN(m_BonusFillUpTime)) {
				if (m_BonusFillUpTime + 2f < Time.time) {
					GameManager.Instance.PushGlobalState(new TowerFinishedLevelState());
				}

				return;
			}

			if (m_LevelData.FallingShape != null)
				return;

			if (IsFilledUpWithBonusBlocks()) {
				m_LevelData.Score.ConsumeBonusScore();
				m_LevelController.ClearSelectedShape();

				m_BonusFillUpTime = Time.time;
				return;
			}

			int rows = m_LevelData.PlayableSize.Row;

			int fallColumnChange = 1;

			// Find available column to drop bonus blocks.
			while (m_LevelData.Grid[rows - 1, (m_LevelData.FallingColumn + fallColumnChange) % m_LevelData.Grid.Columns] != BlockType.None) {
				fallColumnChange++;
			}

			m_LevelController.RequestFallingShapeMove(fallColumnChange);

			var shapeCoords = new List<BlocksShape.ShapeBind>();

			// Select FallShape and rotate to next falling column if needed to.
			int blocksCount = 0;
			for (int row = rows - 1; row >= 0; --row) {

				if (m_LevelData.Grid[row, m_LevelData.FallingColumn] != BlockType.None)
					break;

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = new GridCoords(blocksCount, 0),
					Value = BlockType.WonBonusBlock,
				});

				blocksCount++;
				if (blocksCount >= 3)
					break;


			}

			m_BonusBlocksCount += blocksCount;

			m_LevelController.SelectFallingShape(new BlocksShape() { ShapeCoords = shapeCoords.ToArray() });
			m_LevelController.RequestFallingSpeedUp(m_GameConfig.FallSpeedup * 1.5f);

			m_FlashMessage.ShowMessage($"Blessed!\n{m_BonusBlocksCount}", false);
		}
	}
}