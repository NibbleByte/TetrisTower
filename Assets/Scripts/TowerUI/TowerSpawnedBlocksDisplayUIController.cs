using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerUI
{
	public class TowerSpawnedBlocksDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		public TowerBlockPreviewIcon PreviewIconPrefab;

		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		private FlashMessageUIController m_FlashMessage;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_TowerLevel);
			context.SetByType(out m_FlashMessage);

			m_TowerLevel.FallingShapeSelected += OnFallingShapeSelected;
			m_TowerLevel.SpawnBlockTypesCountChanged += OnSpawnBlockTypesCountChanged;

			// Editor left overs...
			while (transform.childCount > 0) {
				GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
			}

			for(int blockIndex = 0; blockIndex < m_LevelData.NormalBlocksSkins.Length; ++blockIndex) {
				BlockSkin block = m_LevelData.NormalBlocksSkins[blockIndex];
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
				int blockIndex = Array.FindIndex(m_LevelData.NormalBlocksSkins, s => s.BlockType == shapeCoord.Value);

				// Wild or other special block that is not displayed as normal.
				if (blockIndex == -1)
					continue;

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