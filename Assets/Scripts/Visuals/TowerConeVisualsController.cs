using System;
using System.Collections.Generic;
using TetrisTower.TowerLevels;
using TetrisTower.Logic;
using UnityEngine;
using TetrisTower.Core;

namespace TetrisTower.Visuals
{
	public class TowerConeVisualsController : MonoBehaviour
	{
		[Serializable]
		public class VisualsShape : GridShape<GameObject>
		{
		}

		public ConeVisualsGrid VisualsGrid;
		public TowerLevelController TowerLevel;
		public TowerLevelData LevelData => TowerLevel.LevelData;

		public VisualsShape FallingVisualsShape { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private Transform FallingVisualsContainer;

		private void Awake()
		{
			TowerLevel.LevelInitialized += OnLevelInitialized;
			TowerLevel.LevelFallingShapeChanged += ReplaceFallingVisuals;
			TowerLevel.PlacingFallingShape += OnPlacingFallingShape;
			TowerLevel.PlacedOutsideGrid += DestroyFallingVisuals;
			TowerLevel.FallingShapeSelected += OnFallingShapeSelected;

			TowerLevel.FallingShapeRotated += OnFallingShapeRotated;

			FallingVisualsContainer = new GameObject("-- Falling-Shape --").transform;
			FallingVisualsContainer.SetParent(transform);
			FallingVisualsContainer.localPosition = Vector3.zero;
		}

		private void OnLevelInitialized()
		{
			TowerLevel.Grids.Add(VisualsGrid);
			VisualsGrid.Init(LevelData.Grid);

			DestroyFallingVisuals();
			FallingVisualsContainer.SetAsFirstSibling();
		}

		private void ReplaceFallingVisuals()
		{
			DestroyFallingVisuals();

			if (LevelData.FallingShape != null) {
				CreateFallingVisuals();
			}
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

			CreateFallingVisuals();
		}

		private void CreateFallingVisuals()
		{
			var blocksShape = LevelData.FallingShape;

			FallingVisualsShape = m_DebugFallingVisualsShape = new VisualsShape();

			var visualbinds = new List<GridShape<GameObject>.ShapeBind>();

			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab3D, FallingVisualsContainer);
				visualbinds.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));

				int blockColumn = VisualsGrid.WrappedColumn(LevelData.FallingColumn + bind.Coords.Column);

				visualBlock.transform.position = VisualsGrid.ConeApex;
				visualBlock.transform.rotation = VisualsGrid.GridColumnToRotation(blockColumn);
				visualBlock.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - bind.Coords.Row);
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
			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			for(int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				FallingVisualsShape.ShapeCoords[i].Coords = LevelData.FallingShape.ShapeCoords[i].Coords;
			}
		}

		void Update()
		{
			if (FallingVisualsShape != null && !TowerLevel.AreGridActionsRunning) {

				foreach (var shapeCoords in FallingVisualsShape.ShapeCoords) {

					int blockColumn = VisualsGrid.WrappedColumn(LevelData.FallingColumn + shapeCoords.Coords.Column);

					shapeCoords.Value.transform.rotation = VisualsGrid.GridColumnToRotation(blockColumn);
					shapeCoords.Value.transform.localScale = VisualsGrid.GridDistanceToScale(LevelData.FallDistanceNormalized - shapeCoords.Coords.Row);
				}
			}
		}
	}

}