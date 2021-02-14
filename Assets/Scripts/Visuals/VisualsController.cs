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

			var placedCoords = new GridCoords(VisualsGrid.Rows, LevelController.LevelData.FallingColumn);

			foreach (var bind in blocksShape.ShapeCoords) {
				var visualBlock = GameObject.Instantiate(bind.Value.Prefab, transform);
				FallingVisualsShape.ShapeCoords.Add(GridShape<GameObject>.Bind(bind.Coords, visualBlock));

				visualBlock.transform.position = VisualsGrid.GridToWorld(placedCoords + bind.Coords);
			}
		}

		private void DestroyFallingVisuals()
		{
			if (FallingVisualsShape == null)
				return;

			foreach(var bind in FallingVisualsShape.ShapeCoords) {
				GameObject.Destroy(bind.Value);
			}

			FallingVisualsShape = m_DebugFallingVisualsShape = null;
		}
	}

}