using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Pools;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace TetrisTower.Visuals.Environment
{
	public class BurnHotBlocks_LevelBehaviour : MonoBehaviour, ILevelLoadedListener
	{
		private class BurnProgress
		{
			public List<ParticleSystem> BurnEffects = new List<ParticleSystem>();
		}

		public List<ParticlePrefabsPool> EffectsPerTurn = new List<ParticlePrefabsPool>();

		private GridLevelController m_LevelController;
		private BlocksGrid m_Grid => m_LevelController.LevelData.Grid;
		private ConeVisualsGrid m_VisualsGrid;

		// Using the visuals component, we don't have to deal with moving blocks.
		private Dictionary<ConeVisualsBlock, BurnProgress> m_BurningBlocks = new Dictionary<ConeVisualsBlock, BurnProgress>();

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelController);
			context.SetByType(out m_VisualsGrid);

			m_LevelController.FallingShapeSelected += PassBurningTurn;
			m_LevelController.FinishedLevel += OnLevelFinished;
			m_VisualsGrid.DestroyingVisualsBlock += OnDestroyingVisualsBlock;
		}

		public void OnLevelUnloading()
		{
			m_LevelController.FallingShapeSelected -= PassBurningTurn;
			m_LevelController.FinishedLevel -= OnLevelFinished;
			m_VisualsGrid.DestroyingVisualsBlock -= OnDestroyingVisualsBlock;
			m_VisualsGrid = null;

			m_LevelController = null;
		}

		private void OnLevelFinished()
		{
			foreach(var pair in m_BurningBlocks.ToList()) {
				for (int i = 0; i < pair.Value.BurnEffects.Count; i++) {
					EffectsPerTurn[i].Release(pair.Value.BurnEffects[i]);
				}

				m_BurningBlocks.Remove(pair.Key);
			}
		}

		private void OnDestroyingVisualsBlock(ConeVisualsBlock block, GridCoords coords)
		{
			if (m_BurningBlocks.TryGetValue(block, out BurnProgress burnProgress)) {
				for(int i = 0; i < burnProgress.BurnEffects.Count; i++) {
					EffectsPerTurn[i].Release(burnProgress.BurnEffects[i]);
				}

				m_BurningBlocks.Remove(block);
			}
		}

		private bool ShouldStartBurning(int row, int column)
		{
			BlockType blockType = m_LevelController.LevelData.Grid[row, column];
			if (blockType == BlockType.None || blockType == BlockType.StaticBlock || blockType == BlockType.WonBonusBlock)
				return false;

			return row == 0 || m_LevelController.LevelData.Grid[row - 1, column] == BlockType.StaticBlock;
		}

		private void PassBurningTurn()
		{
			if (!m_LevelController.LevelData.IsPlaying)
				return;

			for (int row = 0; row < m_Grid.Rows; ++row) {
				for (int column = 0; column < m_Grid.Columns; ++column) {
					ConeVisualsBlock blockVisuals = m_VisualsGrid[row, column];
					if (blockVisuals == null)
						continue;

					if (!m_BurningBlocks.ContainsKey(blockVisuals) && ShouldStartBurning(row, column)) {
						m_BurningBlocks.Add(blockVisuals, new BurnProgress());
					}
				}
			}

			var replaceBlocks = new List<KeyValuePair<GridCoords, BlockType>>();

			for (int row = 0; row < m_Grid.Rows; ++row) {
				for (int column = 0; column < m_Grid.Columns; ++column) {
					ConeVisualsBlock blockVisuals = m_VisualsGrid[row, column];
					if (blockVisuals == null)
						continue;

					if (m_BurningBlocks.TryGetValue(blockVisuals, out BurnProgress burnProgress)) {
						if (burnProgress.BurnEffects.Count >= EffectsPerTurn.Count) {
							replaceBlocks.Add(KeyValuePair.Create(new GridCoords(row, column), BlockType.StaticBlock));
							// m_BurningBlocks.Remove(blockVisuals); - on destroy will remove it.
							continue;
						}

						ParticleSystem burnEffect = EffectsPerTurn[burnProgress.BurnEffects.Count].Get(transform, worldPositionStays: false);

						burnEffect.transform.position = m_VisualsGrid.GridToWorldBottomCenter(new GridCoords(row, column));
						Vector3 direction = burnEffect.transform.position - m_VisualsGrid.transform.position;
						direction.y = 0;
						burnEffect.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

						burnProgress.BurnEffects.Add(burnEffect);
					}
				}
			}

			if (replaceBlocks.Count > 0) {
				m_LevelController.StartRunActions(new ReplaceCellsAction[] { new ReplaceCellsAction() { ReplacePairs = replaceBlocks } });
			}
		}

		void OnValidate()
		{
		}
	}

}