using System;
using System.Collections.Generic;
using TetrisTower.Levels;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals
{
	public class VisualsController : MonoBehaviour
	{
		[Serializable]
		public class VisualsShape : GridShape<GameObject>
		{
		}

		public VisualsGrid VisualsGrid;
		public LevelController LevelController;
		public LevelData LevelData => LevelController.LevelData;

		public VisualsShape FallingVisualsShape { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private Transform FallingVisualsContainer;

		private void Awake()
		{
			LevelController.LevelInitialized += OnLevelInitialized;
			LevelController.LevelFallingShapeChanged += ReplaceFallingVisuals;
			LevelController.PlacingFallingShape += OnPlacingFallingShape;
			LevelController.PlacedOutsideGrid += DestroyFallingVisuals;
			LevelController.FallingShapeSelected += OnFallingShapeSelected;

			LevelController.FallingShapeRotated += OnFallingShapeRotated;

			FallingVisualsContainer = new GameObject("-- Falling-Shape --").transform;
			FallingVisualsContainer.SetParent(transform);
			FallingVisualsContainer.localPosition = Vector3.zero;
		}

		private void OnLevelInitialized()
		{
			LevelController.Grids.Add(VisualsGrid);
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

			float fallDistance = LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

			var startCoords = new GridCoords(LevelData.Grid.Rows, LevelData.FallingColumn);

			var visualbinds = new List<GridShape<GameObject>.ShapeBind>();

			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab, FallingVisualsContainer);
				visualbinds.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));
				var coords = startCoords + bind.Coords;
				coords.WrapColumn(LevelData.Grid);

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
			Debug.Assert(FallingVisualsShape.ShapeCoords.Length == LevelData.FallingShape.ShapeCoords.Length);

			for(int i = 0; i < FallingVisualsShape.ShapeCoords.Length; ++i) {
				FallingVisualsShape.ShapeCoords[i].Coords = LevelData.FallingShape.ShapeCoords[i].Coords;
			}
		}

		void Update()
		{
			if (FallingVisualsShape != null && !LevelController.AreGridActionsRunning) {
				float fallDistance = LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

				var startCoords = new GridCoords(LevelData.Grid.Rows, LevelData.FallingColumn);

				foreach (var shapeCoords in FallingVisualsShape.AddToCoordsWrapped(startCoords, LevelData.Grid)) {
					shapeCoords.Value.transform.position = VisualsGrid.GridToWorld(shapeCoords.Coords) + Vector3.down * fallDistance;
				}
			}
		}
	}

}