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
		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		public TowerBlockPreviewIcon PreviewIconPrefab;

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

			m_TowerLevel.SpawnBlockTypesCountChanged += RecreatePreview;

			RecreatePreview();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.SpawnBlockTypesCountChanged -= RecreatePreview;
		}

		private void RecreatePreview()
		{
			for(int i = transform.childCount; i < m_LevelData.SpawnBlockTypesCount; ++i) {
				var previewIcon = GameObject.Instantiate(PreviewIconPrefab, transform);

				previewIcon.SetIcon(m_LevelData.SpawnedBlocks[i].Icon);
			}
		}
	}
}