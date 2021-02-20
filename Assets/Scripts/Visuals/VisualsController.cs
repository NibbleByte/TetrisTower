using System;
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
			public Transform Parent;
		}

		public VisualsGrid VisualsGrid;
		public LevelController LevelController;
		public LevelData LevelData => LevelController.LevelData;

		public VisualsShape FallingVisualsShape { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private void Awake()
		{
			LevelController.LevelInitialized += OnLevelInitialized;
			LevelController.LevelFallingShapeChanged += ReplaceFallingVisuals;
			LevelController.PlacingFallingShape += OnPlacingFallingShape;
			LevelController.FallingShapeSelected += OnFallingShapeSelected;
		}

		private void OnLevelInitialized()
		{
			LevelController.Grids.Add(VisualsGrid);
			VisualsGrid.Init(LevelData.Grid);
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
			CreateFallingVisuals();
		}

		private void CreateFallingVisuals()
		{
			var blocksShape = LevelData.FallingShape;

			FallingVisualsShape = m_DebugFallingVisualsShape = new VisualsShape();

			float fallDistance = LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

			var startCoords = new GridCoords(LevelData.Grid.Rows, LevelData.FallingColumn);
			var placedPosition = VisualsGrid.GridToWorld(startCoords) + Vector3.down * fallDistance;

			FallingVisualsShape.Parent = new GameObject("-- Falling-Piece --").transform;
			FallingVisualsShape.Parent.parent = transform;
			FallingVisualsShape.Parent.SetAsFirstSibling();
			FallingVisualsShape.Parent.position = placedPosition;


			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab, FallingVisualsShape.Parent);
				FallingVisualsShape.ShapeCoords.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));

				visualBlock.transform.position = VisualsGrid.GridToWorld(startCoords + bind.Coords) + Vector3.down * fallDistance;
			}
		}

		private void DestroyFallingVisuals()
		{
			if (FallingVisualsShape == null)
				return;

			GameObject.Destroy(FallingVisualsShape.Parent.gameObject);

			FallingVisualsShape = m_DebugFallingVisualsShape = null;
		}

		void Update()
		{
			if (FallingVisualsShape != null) {
				float fallDistance = LevelData.FallDistanceNormalized * VisualsGrid.BlockSize.y;

				var startCoords = new GridCoords(LevelData.Grid.Rows, LevelData.FallingColumn);
				var placedPosition = VisualsGrid.GridToWorld(startCoords) + Vector3.down * fallDistance;

				FallingVisualsShape.Parent.position = placedPosition;
			}
		}
	}

}