using DevLocker.GFrame;
using System;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class TowerNextShapePreviewUIController : MonoBehaviour, ILevelLoadedListener
	{
		public GridLayoutGroup Grid;

		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel.LevelData;

		public TowerBlockPreviewIcon PreviewIconPrefab;

		private void Awake()
		{
			// Editor left overs...
			foreach(Transform child in Grid.transform) {
				child.gameObject.SetActive(true);
			}
		}

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);

			m_TowerLevel.FallingShapeSelected += RecreatePreview;

			RecreatePreview();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.FallingShapeSelected -= RecreatePreview;
		}

		private void RecreatePreview()
		{
			Transform gridTransform = Grid.transform;

			if (m_LevelData?.NextShape == null) {
				foreach(Transform child in gridTransform) {
					DestroyImmediate(child.gameObject);
				}
				return;
			}

			int shapeRows = m_LevelData.NextShape.Rows;
			int shapeColumns = m_LevelData.NextShape.Columns;
			int gridArea = shapeRows * shapeColumns;

			if (gridTransform.childCount != gridArea) {
				Grid.constraintCount = shapeColumns;

				while(gridTransform.childCount > gridArea) {
					var child = gridTransform.GetChild(0);
					GameObject.DestroyImmediate(child.gameObject);
				}

				for (int i = gridTransform.childCount; i < gridArea; ++i) {
					GameObject.Instantiate(PreviewIconPrefab, gridTransform);
				}
			}

			for(int row = 0; row < shapeRows; ++row) {
				for(int column = 0; column < shapeColumns; ++column) {

					Sprite icon = null;

					foreach(var bind in m_LevelData.NextShape.ShapeCoords) {

						var coords = bind.Coords - m_LevelData.NextShape.MinCoords;
						if (coords.Row == row && coords.Column == column) {
							icon = bind.Value.Icon;
							break;
						}
					}

					var previewIcon = gridTransform.GetChild(row * shapeColumns + column).GetComponentInChildren<TowerBlockPreviewIcon>();
					previewIcon.SetIcon(icon);
				}
			}
		}
	}
}