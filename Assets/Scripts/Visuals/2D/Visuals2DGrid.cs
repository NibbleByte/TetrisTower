using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.Visuals2D
{
	public class Visuals2DGrid : MonoBehaviour, GameGrid
	{
		public float BlockMoveSpeed = 1f;
		public Vector2 BlockSize = Vector2.one;

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

		private GridShape<GameObject> m_PlacedShapeToBeReused = null;

		public void Init(BlocksGrid grid)
		{
			if (m_Blocks != null) {
				DestroyInstances();
			}

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

		public Vector3 GridToWorld(GridCoords coords) => new Vector3(coords.Column * BlockSize.x, coords.Row * BlockSize.y);
		public GridCoords WorldToGrid(Vector3 worldPos) => new GridCoords((int) (worldPos.y / BlockSize.y), (int) (worldPos.x / BlockSize.x));

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

						var startPos = GridToWorld(pair.Key);
						var endPos = GridToWorld(pair.Value);

						var pos = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(timePassed / timeNeeded));

						this[pair.Key].transform.localPosition = pos;

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

				this[movedPair.Value].transform.position = GridToWorld(movedPair.Value);
			}

			yield break;
		}

		private void CreateInstanceAt(GridCoords coords, BlockType blockType, GameObject reuseVisuals = null)
		{
			if (reuseVisuals) {
				reuseVisuals.transform.SetParent(transform);
			} else {
				reuseVisuals = GameObject.Instantiate(blockType.Prefab2D, transform);
			}

			// Hitting the limit, won't be stored.
			if (coords.Row < Rows) {
				Debug.Assert(this[coords] == null);
				this[coords] = reuseVisuals;
				reuseVisuals.transform.position = GridToWorld(coords);
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

#if UNITY_EDITOR
		private GUIStyle m_GizmoCoordsStyle;
		[SerializeField]
		private bool m_GizmoShowGrid = true;
		[SerializeField]
		private bool m_GizmoShowGridHeader = true;
		private bool m_GizmoPressed = false;

		void OnDrawGizmos()
		{
			if (Keyboard.current == null)
				return;

			// Because Input.GetKeyDown() doesn't work here :(
			if (!m_GizmoPressed && Keyboard.current.gKey.isPressed) {
				m_GizmoShowGrid = !m_GizmoShowGrid;
				m_GizmoPressed = true;
			}
			if (!m_GizmoPressed && Keyboard.current.hKey.isPressed) {
				m_GizmoShowGridHeader = !m_GizmoShowGridHeader;
				m_GizmoPressed = true;
			}
			if (m_GizmoPressed && !Keyboard.current.gKey.isPressed && !Keyboard.current.hKey.isPressed) {
				m_GizmoPressed = false;
			}

			if (m_GizmoCoordsStyle == null || true) {
				m_GizmoCoordsStyle = new GUIStyle(GUI.skin.label);
				m_GizmoCoordsStyle.alignment = TextAnchor.MiddleCenter;
				m_GizmoCoordsStyle.padding = new RectOffset();
				m_GizmoCoordsStyle.margin = new RectOffset();
				m_GizmoCoordsStyle.contentOffset = new Vector2(-6, -5);
				m_GizmoCoordsStyle.normal.textColor = new Color(0f, 1f, 0f, 0.6f);
			}

			int rows = m_Blocks != null ? Rows : 10;
			int columns = m_Blocks != null ? Columns : 10;

			var coords = new GridCoords();
			var blockHalfSize = new Vector3(BlockSize.x, BlockSize.y) * 0.5f;

			if (m_GizmoShowGridHeader) {
				coords.Column = -1;
				for (coords.Row = 0; coords.Row < rows; ++coords.Row) {
					var position = GridToWorld(coords);

					UnityEditor.Handles.Label(position + blockHalfSize, coords.Row.ToString(), m_GizmoCoordsStyle);
				}

				coords.Row = -1;
				for (coords.Column = 0; coords.Column < columns; ++coords.Column) {
					var position = GridToWorld(coords);

					UnityEditor.Handles.Label(position + blockHalfSize, coords.Column.ToString(), m_GizmoCoordsStyle);
				}
			}

			if (m_GizmoShowGrid) {
				for (coords.Row = 0; coords.Row < rows; ++coords.Row) {
					for (coords.Column = 0; coords.Column < columns; ++coords.Column) {
						var position = GridToWorld(coords);

						UnityEditor.Handles.color = new Color(0, 0, 0, 0.4f);
						UnityEditor.Handles.DrawWireCube(position + blockHalfSize, BlockSize);

						//UnityEditor.Handles.Label(position + blockHalfSize, coords.ToString(), m_GizmoCoordsStyle);

					}
				}
			}
		}
#endif

	}

}