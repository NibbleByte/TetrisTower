using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Visuals grid that displays blocks by scaling up models with focal point - the apex of a cone with n sides.
	/// The apex is pointing up, while the base should lay on the ground below.
	/// 0 scale means block is at the apex, which is at the top.
	/// 1 scale means block is at the lowest row.
	/// NOTE: The top of the grid isn't at the apex of the cone, but the bottom is at the base of it.
	/// </summary>
	public class ConeVisualsGrid : MonoBehaviour, GameGrid
	{
		/*
		 * Sample model creation steps in Blender:
		 * - Create cone: 13 sides, radius 15, depth 100
		 * - Create second cone: radius 12, depth 100
		 * - Generate with Triangle fan for the bottom
		 * - Put the origin at the bottom center.
		 * - Cut out the block, connecting the vertices
		 * - Height of the block - 2.5
		 * - Front bottom edge - 7.17947 (measured)
		 * - Front top edge - 6.99998	(measured)
		 * - Scale change ratio: top / bottom = 0.9749995
		 * - Scale: ratio ^ row
		 *
		 * - Center angle: 27.69 = 360 / 13
		 * - Side angle: 76.155 = 180 - (center angle)
		*/

		// Validation value - cone visuals require different visual models for each size.
		// Supported columns count must be equal to the sides of the cone.
		public int SupportedColumnsCount = 13;

		// Values measured in the model itself.
		public float FrontFaceTopEdgeLength;
		public float FrontFaceBottomEdgeLength;

		// This is used to locate the apex of the cone.
		// The position of this object will be considered the center of the base.
		public float ConeHeight = 100;

		public float BlockMoveSpeed = 1f;

		public float MatchBlockDelay = 0.075f;
		public float MatchActionDelay = 1.2f;

		public int Rows => m_Blocks.GetLength(0);
		public int Columns => m_Blocks.GetLength(1);

		public GameObject this[int row, int column] => m_Blocks[row, column];

		public GameObject this[GridCoords coords] {
			get => m_Blocks[coords.Row, coords.Column];
			private set => m_Blocks[coords.Row, coords.Column] = value;
		}

		private GameObject[,] m_Blocks;

		public Vector3 ConeApex { get; private set; }
		private float m_ConeSectorEulerAngle;
		private float m_ScaleChangeRatio;

		private GridShape<GameObject> m_PlacedShapeToBeReused = null;

		public void Init(BlocksGrid grid)
		{
			if (m_Blocks != null) {
				DestroyInstances();
			}

			if (SupportedColumnsCount != grid.Columns)
				throw new System.ArgumentException($"Supported number of columns for cone visuals is {SupportedColumnsCount} but {grid.Columns} is provided");

			ConeApex = transform.position + Vector3.up * ConeHeight;
			m_ScaleChangeRatio = FrontFaceTopEdgeLength / FrontFaceBottomEdgeLength;
			m_ConeSectorEulerAngle = 360f / grid.Columns;

			m_Blocks = new GameObject[grid.Rows, grid.Columns];

			for(int row = 0; row < grid.Rows; ++row) {
				for(int column = 0; column < grid.Columns; ++column) {
					var coords = new GridCoords(row, column);
					var blockType = grid[coords];

					if (blockType != null) {
						CreateInstanceAt(coords, blockType);
					}
				}
			}
		}

		public Vector3 GridDistanceToScale(float fallDistance) => Vector3.one * Mathf.Pow(m_ScaleChangeRatio, Rows - fallDistance);
		public Vector3 GridToScale(GridCoords coords) => Vector3.one * Mathf.Pow(m_ScaleChangeRatio, coords.Row);

		public Quaternion GridColumnToRotation(int column) => Quaternion.Euler(0f, -m_ConeSectorEulerAngle * column /* Negative because rotation works the other way*/, 0f);

		private void DestroyInstances()
		{
			for (int row = 0; row < Rows; ++row) {
				for (int column = 0; column < Columns; ++column) {
					if (this[row, column]) {
						GameObject.Destroy(this[row, column]);
					}
				}
			}
		}

		public IEnumerator ApplyActions(IEnumerable<GridAction> actions)
		{
			foreach (var action in MergeActions(actions)) {
				switch (action) {
					case PlaceAction placeAction:
						yield return PlaceShape(placeAction);
						break;
					case ClearMatchedAction clearAction:
						yield return ClearMatchedCells(clearAction);
						break;
					case MoveCellsAction moveAction:
						yield return MoveCells(moveAction);
						break;
				}
			}
		}

		private IEnumerable<GridAction> MergeActions(IEnumerable<GridAction> actions)
		{
			List<GridCoords> clearedBlocks = new List<GridCoords>();
			List<KeyValuePair<GridCoords, GridCoords>> mergedMoves = new List<KeyValuePair<GridCoords, GridCoords>>();

			foreach (var action in actions) {

				switch (action) {
					case ClearMatchedAction clearAction:
						clearedBlocks.AddRange(clearAction.Coords);
						break;

					case MoveCellsAction moveAction:
						mergedMoves.AddRange(moveAction.MovedCells);
						break;

					default:
						yield return action;
						break;
				}
			}

			// Merge sequences of clear actions so animations play together.
			if (clearedBlocks.Count > 0) {
				yield return new ClearMatchedAction() { Coords = clearedBlocks.ToArray() };
			}

			// Merge sequences of move actions so animations play together.
			if (mergedMoves.Count > 0) {
				yield return new MoveCellsAction() { MovedCells = mergedMoves.ToArray() };
			}
		}

		private IEnumerator PlaceShape(PlaceAction action)
		{
			foreach (var pair in action.PlacedShape.ShapeCoords) {

				var reusedVisuals = m_PlacedShapeToBeReused?.ShapeCoords
					.Where(sc => sc.Coords == pair.Coords)
					.Select(sc => sc.Value)
					.FirstOrDefault();

				var coords = action.PlaceCoords + pair.Coords;
				coords.WrapColumn(this);

				CreateInstanceAt(coords, pair.Value, reusedVisuals);
			}

			m_PlacedShapeToBeReused = null;

			yield break;
		}

		private IEnumerator ClearMatchedCells(ClearMatchedAction action)
		{
			foreach (var coord in action.Coords) {
				// This can happen if the same block is cleared in different matches.
				//Debug.Assert(this[coord] != null);
				if (this[coord] == null)
					continue;

				GameObject.Destroy(this[coord]);
				this[coord] = null;
				yield return new WaitForSeconds(MatchBlockDelay);
			}

			yield return new WaitForSeconds(MatchActionDelay);
			yield break;
		}

		private IEnumerator MoveCells(MoveCellsAction action)
		{
			if (Application.isPlaying) {

				float startTime = Time.time;
				bool waitingBlocks = true;
				while (waitingBlocks) {
					waitingBlocks = false;

					float timePassed = Time.time - startTime;

					foreach (var pair in action.MovedCells) {
						float distance = GridCoords.Distance(pair.Key, pair.Value);
						float timeNeeded = distance / BlockMoveSpeed;

						var startScale = GridToScale(pair.Key);
						var endScale = GridToScale(pair.Value);

						var scale = Vector3.Lerp(startScale, endScale, Mathf.Clamp01(timePassed / timeNeeded));

						this[pair.Key].transform.localScale = scale;

						if (timePassed < timeNeeded) {
							waitingBlocks = true;
						}
					}

					yield return null;
				}
			}


			foreach (var movedPair in action.MovedCells) {
				Debug.Assert(this[movedPair.Key] != null);
				Debug.Assert(this[movedPair.Value] == null);

				this[movedPair.Value] = this[movedPair.Key];
				this[movedPair.Key] = null;

				this[movedPair.Value].transform.localScale = GridToScale(movedPair.Value);
			}

			yield break;
		}

		private void CreateInstanceAt(GridCoords coords, BlockType blockType, GameObject reuseVisuals = null)
		{
			if (reuseVisuals) {
				reuseVisuals.transform.SetParent(transform);
			} else {
				reuseVisuals = GameObject.Instantiate(blockType.Prefab3D, transform);
			}

			reuseVisuals.transform.position = ConeApex;
			reuseVisuals.transform.rotation = GridColumnToRotation(coords.Column);

			// Hitting the limit, won't be stored.
			if (coords.Row < Rows) {
				Debug.Assert(this[coords] == null);
				this[coords] = reuseVisuals;

				reuseVisuals.transform.localScale = GridToScale(coords);
			} else {
				GameObject.Destroy(reuseVisuals);
			}
		}

		/// <summary>
		/// Shape with GameObject blocks to be reused when placing, instead of creating them from scratch.
		/// Usually placed shape is already visualized as it is falling. Improves performance.
		/// </summary>
		public void SetPlacedShapeToBeReused(GridShape<GameObject> shape)
		{
			m_PlacedShapeToBeReused = shape;
		}
	}

}
