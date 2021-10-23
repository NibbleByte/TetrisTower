using System;
using System.Collections.Generic;
using TetrisTower.TowerLevels;
using TetrisTower.Logic;
using UnityEngine;
using DevLocker.GFrame;

namespace TetrisTower.Visuals2D
{
	public class TowerVisuals2DController : MonoBehaviour, ILevelLoadListener
	{
		[Serializable]
		public class VisualsShape : GridShape<GameObject>
		{
		}

		public Visuals2DGrid VisualsGrid;
		private TowerLevelController m_TowerLevel;
		private TowerLevelData m_LevelData => m_TowerLevel?.LevelData;

		public VisualsShape FallingVisualsShape { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private Transform FallingVisualsContainer;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_TowerLevel);

			m_TowerLevel.PlacingFallingShape += OnPlacingFallingShape;
			m_TowerLevel.PlacedOutsideGrid += DestroyFallingVisuals;
			m_TowerLevel.FallingShapeSelected += OnFallingShapeSelected;

			m_TowerLevel.FallingShapeRotated += OnFallingShapeRotated;

			FallingVisualsContainer = new GameObject("-- Falling-Shape --").transform;
			FallingVisualsContainer.SetParent(transform);
			FallingVisualsContainer.localPosition = Vector3.zero;

			m_TowerLevel.Grids.Add(VisualsGrid);
			VisualsGrid.Init(m_LevelData.Grid);

			DestroyFallingVisuals();
			FallingVisualsContainer.SetAsFirstSibling();

			OnFallingShapeSelected();
		}

		public void OnLevelUnloading()
		{
			m_TowerLevel.PlacingFallingShape -= OnPlacingFallingShape;
			m_TowerLevel.PlacedOutsideGrid -= DestroyFallingVisuals;
			m_TowerLevel.FallingShapeSelected -= OnFallingShapeSelected;

			m_TowerLevel.FallingShapeRotated -= OnFallingShapeRotated;
		}

		private void OnPlacingFallingShape()
		{
			// Instead of destroying blocks now and re-creating them later, try reusing them.
			//DestroyFallingVisuals();
			VisualsGrid.SetPlacedShapeToBeReused(FallingVisualsShape);
		}

		private void OnFallingShapeSelected()
		{
			DestroyFallingVisuals();

			if (m_LevelData.FallingShape != null) {
				CreateFallingVisuals();
			}
		}

		private void CreateFallingVisuals()
		{
			var blocksShape = m_LevelData.FallingShape;

			FallingVisualsShape = m_DebugFallingVisualsShape = new VisualsShape();

			float fallDistance = m_LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

			var startCoords = new GridCoords(m_LevelData.Grid.Rows, m_LevelData.FallingColumn);

			var visualbinds = new List<GridShape<GameObject>.ShapeBind>();

			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab2D, FallingVisualsContainer);
				visualbinds.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));
				var coords = startCoords + bind.Coords;
				coords.WrapColumn(m_LevelData.Grid);

				visualBlock.transform.position = VisualsGrid.GridToWorld(coords) + Vector3.down * fallDistance;
			}

			FallingVisualsShape.ShapeCoords = visualbinds.ToArray();
		}

		private void DestroyFallingVisuals()
		{
			foreach (Transform child in FallingVisualsContainer) {
				GameObject.Destroy(child.gameObject);
			}

			FallingVisualsShape = m_DebugFallingVisualsShape = null;
		}

		private void OnFallingShapeRotated()
		{
			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == m_LevelData.FallingShape.ShapeCoords.Length);

			for(int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				FallingVisualsShape.ShapeCoords[i].Coords = m_LevelData.FallingShape.ShapeCoords[i].Coords;
			}
		}

		void Update()
		{
			if (FallingVisualsShape != null && !m_TowerLevel.AreGridActionsRunning) {
				float fallDistance = m_LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

				var startCoords = new GridCoords(m_LevelData.Grid.Rows, m_LevelData.FallingColumn);

				foreach (var shapeCoords in FallingVisualsShape.AddToCoordsWrapped(startCoords, m_LevelData.Grid)) {
					shapeCoords.Value.transform.position = VisualsGrid.GridToWorld(shapeCoords.Coords) + Vector3.down * fallDistance;
				}
			}
		}
	}

}