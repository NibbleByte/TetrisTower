using DevLocker.GFrame;
using System.Collections.Generic;
using System.Threading.Tasks;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.UI;
using UnityEngine.InputSystem;

namespace TetrisTower.TowerLevels
{
	public class TowerWonState : ILevelState, PlayerControls.ICommonHotkeysActions, IUpdateListener
	{
		private GameConfig m_GameConfig;
		private PlayerControls m_PlayerControls;
		private TowerLevelUIController m_UIController;
		private GridLevelController m_LevelController;
		private GridLevelData m_LevelData => m_LevelController.LevelData;

		private FlashMessageUIController m_FlashMessage;

		private int m_BonusBlocksCount = 0;

		public Task EnterStateAsync(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_GameConfig);
			contextReferences.SetByType(out m_UIController);
			contextReferences.SetByType(out m_PlayerControls);
			contextReferences.SetByType(out m_LevelController);
			contextReferences.SetByType(out m_FlashMessage);


			m_PlayerControls.InputStack.PushActionsState(this);
			m_PlayerControls.UI.Enable();
			m_PlayerControls.CommonHotkeys.SetCallbacks(this);
			m_PlayerControls.CommonHotkeys.Enable();
			// HACK: Listen for touch-screen tapping as they don't have CommonHotkeys (back button is not ideal).
			m_PlayerControls.TowerLevelPlay.PointerPress.performed += OnPointerPressed;
			m_PlayerControls.TowerLevelPlay.PointerPress.Enable();

			// These are still active (Menu hotkey).
			m_PlayerControls.TowerLevelShared.Disable();

			m_UIController.SwitchState(TowerLevelUIState.Play);

			if (m_LevelData.FallingShape != null) {
				// HACK: This happens only via cheats. This will eventually the currently displayed falling shape.
				m_LevelData.FallingShape = null;
				Update();
			}

			return Task.CompletedTask;
		}

		public Task ExitStateAsync()
		{
			m_PlayerControls.TowerLevelPlay.PointerPress.performed -= OnPointerPressed;
			m_PlayerControls.CommonHotkeys.SetCallbacks(null);
			m_PlayerControls.InputStack.PopActionsState(this);

			return Task.CompletedTask;
		}

		public void OnBack(InputAction.CallbackContext context)
		{
			InterruptAnimation();
			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		public void OnConfirm(InputAction.CallbackContext context)
		{
			InterruptAnimation();
			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
		}

		private void OnPointerPressed(InputAction.CallbackContext obj)
		{
			InterruptAnimation();
			GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
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
					if (m_LevelData.Grid[row, column] == null) {
						shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
							Coords = new GridCoords(row, column),
							Value = m_GameConfig.WonBonusBlock,
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
				if (m_LevelData.Grid[m_LevelData.PlayableSize.Row - 1, column] == null)
					return false;
			}

			return true;
		}

		public void Update()
		{
			if (m_LevelData.FallingShape != null)
				return;

			if (IsFilledUpWithBonusBlocks()) {
				m_LevelData.Score.ConsumeBonusScore();
				m_LevelController.ClearSelectedShape();

				GameManager.Instance.PushLevelState(new TowerFinishedLevelState());
				return;
			}

			int rows = m_LevelData.PlayableSize.Row;

			int fallColumnChange = 1;

			// Find available column to drop bonus blocks.
			while (m_LevelData.Grid[rows - 1, (m_LevelData.FallingColumn + fallColumnChange) % m_LevelData.Grid.Columns] != null) {
				fallColumnChange++;
			}

			m_LevelController.RequestFallingShapeMove(fallColumnChange);

			var shapeCoords = new List<BlocksShape.ShapeBind>();

			// Select FallShape and rotate to next falling column if needed to.
			int blocksCount = 0;
			for (int row = rows - 1; row >= 0; --row) {

				if (m_LevelData.Grid[row, m_LevelData.FallingColumn])
					break;

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = new GridCoords(blocksCount, 0),
					Value = m_GameConfig.WonBonusBlock,
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