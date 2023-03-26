using System;
using TetrisTower.Logic;
using UnityEngine;
using DevLocker.GFrame;
using TetrisTower.Core;
using DevLocker.GFrame.Input;

namespace TetrisTower.Visuals.Effects
{
	public class TowerConeFallPreviewController : MonoBehaviour, ILevelLoadedListener
	{
		public ConeVisualsGrid VisualsGrid;

		public GameObject BlockPreviewPrefab;

		private GridLevelController m_TowerLevel;
		private GridLevelData m_LevelData => m_TowerLevel?.LevelData;

		private Renderer m_PreviewRenderer;
		private Material m_PreviewMaterial;

		private int m_CurrentPreviewColumn = 0;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			if (!isActiveAndEnabled)
				return;

			// Assembly non-reload may preserve this, no?
			if (m_PreviewRenderer == null) {
				m_PreviewRenderer = Instantiate(BlockPreviewPrefab, transform).GetComponent<Renderer>();
			}

			m_PreviewRenderer.transform.position = VisualsGrid.ConeApex;
			m_PreviewRenderer.gameObject.SetActive(false);
			m_PreviewRenderer.name = "-- Fall Preview Block --";
			m_PreviewMaterial = m_PreviewRenderer.material = new Material(m_PreviewRenderer.sharedMaterial);

			context.SetByType(out m_TowerLevel);

			m_TowerLevel.FallingColumnChanged += OnFallingColumnChanged;
			m_TowerLevel.FallingShapeSelected += OnFallingShapeSelected;

			m_PreviewRenderer.gameObject.SetActive(m_LevelData.FallingShape != null);

			if (m_LevelData.FallingShape != null) {
				OnFallingShapeSelected();
			}
		}

		public void OnLevelUnloading()
		{
			if (!isActiveAndEnabled)
				return;

			GameObject.DestroyImmediate(m_PreviewRenderer.gameObject);

			m_TowerLevel.FallingColumnChanged -= OnFallingColumnChanged;
			m_TowerLevel.FallingShapeSelected -= OnFallingShapeSelected;
		}

		private void OnFallingShapeSelected()
		{
			if (!m_LevelData.IsPlaying)
				return;

			UpdatePreviewPosition();
		}

		private void OnFallingColumnChanged()
		{
			UpdatePreviewPosition();
		}

		private void UpdatePreviewPosition()
		{
			m_CurrentPreviewColumn = m_LevelData.FallingColumn;

			int row;
			for(row = m_LevelData.Grid.Rows - 1; row >= 0; --row) {
				if (m_LevelData.Grid[row, m_CurrentPreviewColumn] != BlockType.None)
					break;
			}

			int freeRow = row + 1;
			m_PreviewRenderer.transform.localScale = VisualsGrid.GridToScale(new GridCoords(freeRow, m_CurrentPreviewColumn));
			m_PreviewRenderer.transform.localRotation = VisualsGrid.GridColumnToRotation(m_CurrentPreviewColumn);
		}

		void Update()
		{
			if (m_LevelData == null || m_PreviewRenderer == null)
				return;

			if (m_LevelData.FallingShape == null || !m_LevelData.IsPlaying) {
				if (m_PreviewRenderer.gameObject.activeSelf) {
					m_PreviewRenderer.gameObject.SetActive(false);
				}

				return;

			} else if (!m_PreviewRenderer.gameObject.activeSelf) {
				m_PreviewRenderer.gameObject.SetActive(true);
			}

			float startScale = VisualsGrid.GridDistanceToScale(0f).x;
			float previewScale = m_PreviewRenderer.transform.localScale.x;
			float fallScale = VisualsGrid.GridDistanceToScale(m_LevelData.FallDistanceNormalized).x;

			float fallScaleRemapped = MathUtils.Remap(fallScale, startScale, 1f, 0f, 1f);
			float previewScaleRemapped = MathUtils.Remap(previewScale, startScale, 1f, 0f, 1f);

			m_PreviewMaterial.SetFloat("Visibility", 1f - fallScaleRemapped / previewScaleRemapped);
		}
	}

}