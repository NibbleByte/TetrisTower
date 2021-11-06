using DevLocker.GFrame;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class TowerSpawnedBlocksDisplayUIController : MonoBehaviour, ILevelLoadedListener
	{
		private TowerLevelController m_TowerLevel;
		private TowerLevelData m_LevelData => m_TowerLevel.LevelData;

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

			// TODO: Subscribe for added block types

			RecreatePreview();
		}

		public void OnLevelUnloading()
		{
			// TODO: Unsubscribe for added block types
		}

		private void RecreatePreview()
		{
			for(int i = transform.childCount; i < m_LevelData.SpawnedBlocks.Length; ++i) {
				var previewIcon = GameObject.Instantiate(PreviewIconPrefab, transform);

				previewIcon.SetIcon(m_LevelData.SpawnedBlocks[i].Icon);
			}
		}
	}
}