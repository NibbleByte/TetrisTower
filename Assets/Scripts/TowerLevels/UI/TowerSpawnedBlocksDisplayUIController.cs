using DevLocker.GFrame;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class TowerSpawnedBlocksDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		public TowerBlockPreviewIcon PreviewIconPrefab;

		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private FlashMessageUIController m_FlashMessage;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);
			contextReferences.SetByType(out m_FlashMessage);

			m_TowerLevel.FallingShapeSelected += OnFallingShapeSelected;
			m_TowerLevel.SpawnBlockTypesCountChanged += OnSpawnBlockTypesCountChanged;

			// Editor left overs...
			while (transform.childCount > 0) {
				GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
			}

			for(int blockIndex = 0; blockIndex < m_LevelData.BlocksPool.Length; ++blockIndex) {
				BlockType block = m_LevelData.BlocksPool[blockIndex];
				TowerBlockPreviewIcon previewIcon = GameObject.Instantiate(PreviewIconPrefab, transform);

				previewIcon.gameObject.SetActive(ShouldBeActive(blockIndex));

				previewIcon.SetIcon(block.Icon);
			}
		}

		public void OnLevelUnloading()
		{
		}

		private bool ShouldBeActive(int blockIndex)
		{
			return m_LevelData.SpawnBlockTypesRange.x <= blockIndex && blockIndex < m_LevelData.SpawnBlockTypesRange.y;
		}

		private void OnFallingShapeSelected()
		{
			if (!m_LevelData.IsPlaying)
				return;

			BlocksShape shape = m_LevelData.NextShape;

			if (shape == null) {
				Debug.LogWarning("No next shape selected?", this);
				shape = m_LevelData.FallingShape;
				if (shape == null) {
					Debug.LogError("No falling shape selected?", this);
					return;
				}
			}

			foreach (var shapeCoord in shape.ShapeCoords) {
				int blockIndex = Array.IndexOf(m_LevelData.BlocksPool, shapeCoord.Value);
				GameObject previewIcon = transform.GetChild(blockIndex).gameObject;

				if (!previewIcon.activeSelf && ShouldBeActive(blockIndex)) {
					previewIcon.SetActive(true);
					m_FlashMessage.ShowMessage("New Block");
				}
			}
		}

		private void OnSpawnBlockTypesCountChanged()
		{
			for (int blockIndex = 0; blockIndex < transform.childCount; ++blockIndex) {
				GameObject previewIcon = transform.GetChild(blockIndex).gameObject;

				// Check only for obsolete blocks. Added new blocks are checked on shape select.
				if (previewIcon.activeSelf && !ShouldBeActive(blockIndex)) {
					previewIcon.SetActive(false);
					m_FlashMessage.ShowMessage("Obsolete Block");
				}
			}
		}
	}
}