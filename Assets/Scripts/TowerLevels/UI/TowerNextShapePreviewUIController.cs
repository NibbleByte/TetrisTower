using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class TowerNextShapePreviewUIController : MonoBehaviour
	{
		public GridLayoutGroup Grid;

		public TowerLevelController TowerLevel;
		public TowerLevelData LevelData => TowerLevel.LevelData;

		public TowerNextShapePreviewIcon PreviewIconPrefab;

		private void Awake()
		{
			// Editor left overs...
			foreach(Transform child in Grid.transform) {
				child.gameObject.SetActive(true);
			}
		}

		void OnEnable()
		{
			TowerLevel.LevelInitialized += RecreatePreview;
			TowerLevel.FallingShapeSelected += RecreatePreview;

			// TODO: This is bad design.
			if (LevelData != null) {
				RecreatePreview();
			}
		}

		void OnDisable()
		{
			if (TowerLevel) {
				TowerLevel.LevelInitialized -= RecreatePreview;
				TowerLevel.FallingShapeSelected -= RecreatePreview;
			}
		}

		private void RecreatePreview()
		{
			Transform gridTransform = Grid.transform;

			if (LevelData?.NextShape == null) {
				foreach(Transform child in gridTransform) {
					DestroyImmediate(child.gameObject);
				}
				return;
			}

			int shapeRows = LevelData.NextShape.Rows;
			int shapeColumns = LevelData.NextShape.Columns;
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

					foreach(var bind in LevelData.NextShape.ShapeCoords) {

						var coords = bind.Coords - LevelData.NextShape.MinCoords;
						if (coords.Row == row && coords.Column == column) {
							icon = bind.Value.Icon;
							break;
						}
					}

					var previewIcon = gridTransform.GetChild(row * shapeColumns + column).GetComponentInChildren<TowerNextShapePreviewIcon>();
					previewIcon.SetIcon(icon);
				}
			}
		}
	}
}