using System.Collections;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Levels
{
	public class LevelController : MonoBehaviour
	{
		public LevelData LevelData { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private LevelData m_DebugLevelData;


		public List<GameGrid> Grids { get; private set; } = new List<GameGrid>();

		public event System.Action LevelInitialized;
		public event System.Action LevelFallingShapeChanged;
		public event System.Action PlacingFallingShape;
		public event System.Action PlacedOutsideGrid;
		public event System.Action FallingShapeSelected;

		private int m_NextRunId = 0;
		private int m_FinishedRuns = 0;

		public bool AreGridActionsRunning => m_FinishedRuns != m_NextRunId;

		private float m_FallingSpeedup = 0;

		public void Init(LevelData data)
		{
			LevelData = m_DebugLevelData = data;

			Grids.Clear();
			Grids.Add(LevelData.Grid);

			LevelInitialized?.Invoke();
			LevelFallingShapeChanged?.Invoke();
		}

		public IEnumerator RunActions(IList<GridAction> actions)
		{
			var currentRunId = m_NextRunId;
			m_NextRunId++;

			while (m_FinishedRuns < currentRunId)
				yield return null;


			while (actions.Count > 0) {

				foreach (var grid in Grids) {

					IEnumerable<GridAction> transformedActions = actions;
					if (grid is GridActionsTransformer transformer) {
						transformedActions = transformer.Transform(actions);
					}

					foreach (var action in transformedActions) {
						yield return action.Apply(grid);
					}
				}

				actions = GameGridEvaluation.Evaluate(LevelData.Grid, LevelData.Rules);
			}

			m_FinishedRuns++;
		}

		public void SelectFallingShape()
		{
			LevelData.FallingShape = LevelData.NextShape;
			LevelData.FallDistanceNormalized = 0f;

			GridShapeTemplate template = LevelData.ShapeTemplates[Random.Range(0, LevelData.ShapeTemplates.Length)];
			LevelData.NextShape = GenerateShape(template, LevelData.SpawnedBlocks);

			FallingShapeSelected?.Invoke();
		}

		public bool RequestFallingShapeOffset(int offsetColumns)
		{
			if (!LevelData.HasFallingShape)
				return false;

			int requestedColumn = Mathf.Clamp(LevelData.FallingColumn + offsetColumns, 0, LevelData.Grid.Columns - LevelData.FallingShape.Columns);

			// NOTE: This won't check in-between cells and jump over occupied ones.
			var fallCoords = LevelData.CalcFallShapeCoordsAt(requestedColumn);

			foreach (var pair in LevelData.FallingShape.ShapeCoords) {
				var coords = fallCoords + pair.Coords;

				if (coords.Row < 0 || coords.Row >= LevelData.Grid.Rows)
					continue;

				if (LevelData.Grid[coords])
					return false;
			}

			LevelData.FallingColumn = requestedColumn;

			return true;
		}

		public bool RequestFallingSpeedUp(float speedUp)
		{
			if (!LevelData.HasFallingShape)
				return false;

			m_FallingSpeedup = speedUp;

			return true;
		}

		private IEnumerator PlaceFallingShape(GridCoords placeCoords, BlocksShape placedShape)
		{
			var actions = new List<GridAction>() {
				new PlaceAction() { PlaceCoords = placeCoords, PlacedShape = placedShape }
			};

			yield return RunActions(actions);

			// Limit was reached, game over.
			if (placeCoords.Row + placedShape.Rows - 1 < LevelData.Grid.Rows) {
				SelectFallingShape();
			} else {
				Debug.Log($"Placed shape with size ({placedShape.Rows}, {placedShape.Columns}) at {placeCoords}, but that goes outside the grid ({LevelData.Grid.Rows}, {LevelData.Grid.Columns}). It will be trimmed!");
				PlacedOutsideGrid?.Invoke();
			}
		}

		private static BlocksShape GenerateShape(GridShapeTemplate template, BlockType[] spawnBlocks)
		{
			var shapeCoords = new List<BlocksShape.ShapeBind>();
			foreach(var coords in template.ShapeTemplate) {
				var blockType = spawnBlocks[Random.Range(0, spawnBlocks.Length)];

				shapeCoords.Add(new GridShape<BlockType>.ShapeBind() {
					Coords = coords,
					Value = blockType,
				});
			}

			return new BlocksShape() { ShapeCoords = shapeCoords };
		}

		void Update()
		{
			if (LevelData.HasFallingShape) {
				UpdateFallShape();

			} else if (!AreGridActionsRunning && LevelData.Grid[LevelData.Grid.Rows - 1, LevelData.FallingColumn] == null) {
				SelectFallingShape();
			}

			DebugClearRow();
		}

		private void UpdateFallShape()
		{
			// Clamp to avoid skipping over a cell on hiccups.
			LevelData.FallDistanceNormalized += Mathf.Clamp01(Time.deltaTime * (LevelData.FallSpeedNormalized + m_FallingSpeedup));

			var fallCoords = LevelData.FallShapeCoords;

			foreach (var pair in LevelData.FallingShape.ShapeCoords) {
				var coords = fallCoords + pair.Coords;

				if (coords.Row >= LevelData.Grid.Rows)
					continue;

				if (coords.Row < 0 || LevelData.Grid[coords]) {

					PlacingFallingShape?.Invoke();

					// The current coords are taken so move back one tile up where it should be free.
					fallCoords.Row++;

					m_FallingSpeedup = 0f;

					var fallShape = LevelData.FallingShape;
					LevelData.FallingShape = null;

					StartCoroutine(PlaceFallingShape(fallCoords, fallShape));
					break;
				}
			}
		}

		private void DebugClearRow()
		{
#if UNITY_EDITOR
			for (KeyCode key = KeyCode.Alpha0; key <= KeyCode.Alpha9; ++key) {
				if (Input.GetKeyDown(key) && key - KeyCode.Alpha0 < LevelData.Grid.Rows) {

					List<GridCoords> clearCoords = new List<GridCoords>();
					for (int column = 0; column < LevelData.Grid.Columns; ++column) {
						var coords = new GridCoords(key - KeyCode.Alpha0, column);
						if (LevelData.Grid[coords]) {
							clearCoords.Add(coords);
						}
					}

					if (clearCoords.Count > 0) {
						var actions = new List<GridAction>() { new ClearMatchedAction() { Coords = clearCoords } };
						StartCoroutine(RunActions(actions));
					}
				}
			}
#endif
		}
	}
}