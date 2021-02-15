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

		public VisualsShape FallingVisualsShape { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private VisualsShape m_DebugFallingVisualsShape;

		private void Awake()
		{
			LevelController.LevelInitialized += OnLevelInitialized;
			LevelController.LevelFallingShapeChanged += ReplaceFallingVisuals;
		}

		private void OnLevelInitialized()
		{
			LevelController.Grids.Add(VisualsGrid);
			VisualsGrid.Init(LevelController.LevelData.Grid);
		}

		private void ReplaceFallingVisuals()
		{
			DestroyFallingVisuals();

			if (LevelController.LevelData.FallingShape != null) {
				CreateFallingVisuals();
			}
		}

		private void CreateFallingVisuals()
		{
			var blocksShape = LevelController.LevelData.FallingShape;

			FallingVisualsShape = m_DebugFallingVisualsShape = new VisualsShape();

			var placedCoords = LevelController.LevelData.FallingCoords;

			FallingVisualsShape.Parent = new GameObject("-- Falling-Piece --").transform;
			FallingVisualsShape.Parent.parent = transform;
			FallingVisualsShape.Parent.SetAsFirstSibling();
			FallingVisualsShape.Parent.position = VisualsGrid.GridToWorld(placedCoords);


			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab, FallingVisualsShape.Parent);
				FallingVisualsShape.ShapeCoords.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));

				visualBlock.transform.position = VisualsGrid.GridToWorld(placedCoords + bind.Coords);
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
				var move = Vector3.down * Time.deltaTime * LevelController.LevelData.FallSpeed;
				var position = FallingVisualsShape.Parent.position + move;
				FallingVisualsShape.Parent.position = position;

				LevelController.LevelData.FallingCoords = VisualsGrid.WorldToGrid(position);
			}
		}
	}

}