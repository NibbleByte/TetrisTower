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
		private BlockType[] m_DisplayedBlocks;

		private void Awake()
		{
			// Editor left overs...
			while(transform.childCount > 0) {
				GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
			}
		}

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);
			contextReferences.SetByType(out m_FlashMessage);

			m_TowerLevel.FallingShapeSelected += OnFallingShapeSelected;

			RecreatePreview();
		}

		public void OnLevelUnloading()
		{
		}

		private void OnFallingShapeSelected()
		{
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
				if (!m_DisplayedBlocks.Contains(shapeCoord.Value)) {
					RecreatePreview();
					m_FlashMessage.ShowMessage("New Block");
				}
			}
		}

		private void RecreatePreview()
		{
			m_DisplayedBlocks = m_LevelData.SpawnedBlocks.Take(m_LevelData.SpawnBlockTypesCount).ToArray();

			for (int i = transform.childCount; i < m_DisplayedBlocks.Length; ++i) {
				var previewIcon = GameObject.Instantiate(PreviewIconPrefab, transform);

				previewIcon.SetIcon(m_DisplayedBlocks[i].Icon);
			}
		}
	}
}