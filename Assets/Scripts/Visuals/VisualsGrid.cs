using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals
{
	public class VisualsGrid : MonoBehaviour, GameGrid, GridActionsTransformer
	{
		public float BlockMoveSpeed = 1f;
		public Vector2 BlockSize = Vector2.one;

		public int Rows => m_Blocks.GetLength(0);
		public int Columns => m_Blocks.GetLength(1);

		public GameObject this[int row, int column] => m_Blocks[row, column];

		public GameObject this[GridCoords coords] {
			get => m_Blocks[coords.Row, coords.Column];
			private set => m_Blocks[coords.Row, coords.Column] = value;
		}

		private GameObject[,] m_Blocks;


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

		public void PlaceCells(IReadOnlyCollection<KeyValuePair<GridCoords, BlockType>> placeCells)
		{
			foreach (var pair in placeCells) {
				CreateInstanceAt(pair.Key, pair.Value);
			}
		}

		public IEnumerator ClearMatchedCells(IReadOnlyCollection<GridCoords> coords)
		{
			foreach (var coord in coords) {
				Debug.Assert(this[coord] != null);

				GameObject.Destroy(this[coord]);
				this[coord] = null;
			}

			yield break;
		}

		public IEnumerator MoveCells(IReadOnlyCollection<KeyValuePair<GridCoords, GridCoords>> movedCells)
		{
			if (Application.isPlaying) {

				float startTime = Time.time;
				bool waitingBlocks = true;
				while (waitingBlocks) {
					waitingBlocks = false;

					float timePassed = Time.time - startTime;

					foreach (var pair in movedCells) {
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


			foreach (var movedPair in movedCells) {
				Debug.Assert(this[movedPair.Key] != null);
				Debug.Assert(this[movedPair.Value] == null);

				this[movedPair.Value] = this[movedPair.Key];
				this[movedPair.Key] = null;

				this[movedPair.Value].transform.position = GridToWorld(movedPair.Value);
			}

			yield break;
		}

		private void CreateInstanceAt(GridCoords coords, BlockType blockType)
		{
			Debug.Assert(this[coords] == null);

			var instance = GameObject.Instantiate(blockType.Prefab, transform);
			this[coords] = instance;

			instance.transform.position = GridToWorld(coords);
		}

		public IEnumerable<GridAction> Transform(IEnumerable<GridAction> actions)
		{
			List<KeyValuePair< GridCoords, GridCoords >> mergedMoves = new List<KeyValuePair<GridCoords, GridCoords>>();

			foreach(var action in actions) {

				if (action is MoveCellsAction moveAction) {
					mergedMoves.AddRange(moveAction.MovedCells);

				} else {
					// Merge sequences of MoveActions so animations play together.
					if (mergedMoves.Count > 0) {
						yield return new MoveCellsAction() { MovedCells = mergedMoves.ToArray() };
						mergedMoves.Clear();
					}

					yield return action;
				}
			}

			if (mergedMoves.Count > 0) {
				yield return new MoveCellsAction() { MovedCells = mergedMoves.ToArray() };
			}
		}
	}

}