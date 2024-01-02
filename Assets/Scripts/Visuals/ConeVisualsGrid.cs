using DevLocker.GFrame.Input;
using DevLocker.GFrame.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
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
		[Header("Dimensions")]
		[Range(Game.LevelParamData.SupportedColumnsCount, Game.LevelParamData.SupportedColumnsCount)]
		public int SupportedColumnsCount = Game.LevelParamData.SupportedColumnsCount;

		// Values measured in the model itself.
		public float FrontFaceTopEdgeLength;
		public float FrontFaceBottomEdgeLength;

		public float BlockHeight = 2.5f;
		public float BlockDepth = 2.84f;
		public float ConeOuterRadius = 15f;
		public float ConeInnerRadius = 12f;

		// This is used to locate the apex of the cone.
		// The position of this object will be considered the center of the base.
		public float ConeHeight = 100;

		[Header("Pacing")]
		public float BlockMoveSpeed = 1f;

		public float MatchBlockDelay = 0.075f;
		public float MatchActionDelay = 1.2f;

		[Header("Effects")]
		public ParticleSystem FallHitEffect;
		public ParticleSystem MatchBlockEffect;
		public Effects.FallTrailEffectsManager FallTrailEffectsManager;
		public Vector2 PushUpWarningShakeRange = new Vector2(-0.1f, 0.1f);

		public delegate void ScoreEventHandler(ScoreGrid scoreGrid);
		public event ScoreEventHandler ScoreUpdated;
		public event ScoreEventHandler ScoreFinished;

		public int Rows => m_Blocks.GetLength(0);
		public int Columns => m_Blocks.GetLength(1);

		public ConeVisualsBlock this[int row, int column] {
			get => m_Blocks[row, column];
			private set => m_Blocks[row, column] = value;
		}

		public ConeVisualsBlock this[GridCoords coords] {
			get => m_Blocks[coords.Row, coords.Column];
			private set => m_Blocks[coords.Row, coords.Column] = value;
		}

		public IReadOnlyList<ConeVisualsBlock> AllBlocks => m_AllBlocks;
		private List<ConeVisualsBlock> m_AllBlocks = new();

		public delegate void VisualsBlockEventHandler(ConeVisualsBlock block);
		public event VisualsBlockEventHandler CreatedVisualsBlock;
		public event VisualsBlockEventHandler DestroyingVisualsBlock;

		private ConeVisualsBlock[,] m_Blocks;

		public Vector3 ConeApex { get; private set; }
		public float ConeSectorEulerAngle { get; private set; }
		private float m_ScaleChangeRatio;

		private GridShape<GameObject> m_PlacedShapeToBeReused = null;

		private BlocksSkinStack m_BlocksSkinStack;
		private GridRules m_Rules;
		private ScoreGrid m_ScoreGrid = null;

		public System.Random VisualsRandom { get; private set; }

		public GridAction CurrentlyRunningAction { get; private set; }

		private GridCoords m_PlayableArea;

		private Effects.FairyMatchingController m_Fairy;
		private Effects.TowerCameraEffects m_CameraEffects;

		private int m_BlocksLayer;

		public void Init(PlayerStatesContext context, System.Random visualsRandom, int blocksLayer)
		{
			GridLevelController towerLevel = context.FindByType<GridLevelController>();
			VisualsRandom = visualsRandom;
			m_BlocksLayer = blocksLayer;
			FallTrailEffectsManager.BlocksLayer = m_BlocksLayer;

			BlocksGrid grid = towerLevel.Grid;

			if (m_Blocks != null) {
				DestroyInstances();
			}

			if (SupportedColumnsCount != grid.Columns)
				throw new System.ArgumentException($"Supported number of columns for cone visuals is {SupportedColumnsCount} but {grid.Columns} is provided");

			CalculateCone(grid.Columns);

			m_BlocksSkinStack = towerLevel.LevelData.BlocksSkinStack;
			m_Rules = towerLevel.LevelData.Rules;

			m_Blocks = new ConeVisualsBlock[grid.Rows, grid.Columns];
			m_ScoreGrid = null;

			m_PlayableArea = towerLevel.LevelData.PlayableSize;

			for (int row = 0; row < grid.Rows; ++row) {
				for(int column = 0; column < grid.Columns; ++column) {
					var coords = new GridCoords(row, column);
					var blockType = grid[coords];

					if (blockType != BlockType.None) {
						CreateInstanceAt(coords, blockType);
					}
				}
			}

			context.SetByType(out m_CameraEffects);

			if (context.TrySetByType(out m_Fairy)) {

				// TODO: The order of this is uncurtain.
				GameObject[] found = GameObject.FindGameObjectsWithTag(Game.GameTags.FairyRestPoint);
				var fairyRestPoints = found.Where(go => go.transform.IsChildOf(transform)).Select(go => go.transform).ToArray();

				if (fairyRestPoints.Length >= 2) {
					m_Fairy.Init(fairyRestPoints, transform.position, ConeOuterRadius, visualsRandom);
				} else {
					Debug.LogWarning("Couldn't find enough fairy rest point. Need at least 2. Destroying the fairy.", this);

					context.RemoveByType<Effects.FairyMatchingController>();
					Destroy(m_Fairy.gameObject);
					m_Fairy = null;
				}
			}
		}

		private void CalculateCone(int columns)
		{
			ConeApex = transform.position + Vector3.up * ConeHeight;
			m_ScaleChangeRatio = FrontFaceTopEdgeLength / FrontFaceBottomEdgeLength;
			ConeSectorEulerAngle = 360f / columns;
		}

		public Vector3 GridDistanceToScale(float fallDistance) => Vector3.one * Mathf.Pow(m_ScaleChangeRatio, Rows - fallDistance);
		public Vector3 GridToScale(GridCoords coords) => Vector3.one * Mathf.Pow(m_ScaleChangeRatio, coords.Row);

		public Quaternion GridColumnToRotation(int column) => Quaternion.Euler(0f, -ConeSectorEulerAngle * column /* Negative because rotation works the other way*/, 0f);


		// Back bottom left vertex of the block.
		public Vector3 GridToWorldBackVertex(GridCoords coords)
		{
			var baseVertex = transform.position
				+ Quaternion.Euler(0f, -ConeSectorEulerAngle * coords.Column + ConeSectorEulerAngle / 2f, 0f)
				* transform.forward * ConeInnerRadius
				;

			var coneEdgeFullDist = baseVertex - ConeApex;
			return ConeApex + coneEdgeFullDist * GridToScale(coords).x;
		}

		// Front bottom left vertex of the block.
		public Vector3 GridToWorldFrontVertex(GridCoords coords)
		{
			var baseVertex = transform.position
				+ Quaternion.Euler(0f, -ConeSectorEulerAngle * coords.Column + ConeSectorEulerAngle / 2f, 0f)
				* transform.forward * ConeOuterRadius
				;

			var coneEdgeFullDist = baseVertex - ConeApex;
			return ConeApex + coneEdgeFullDist * GridToScale(coords).x;
		}

		public Vector3 GridToWorldBackEdgeMidpoint(GridCoords coords)
		{
			var vertexStart = GridToWorldBackVertex(coords);
			coords.Column++;
			var vertexEnd = GridToWorldBackVertex(coords);

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public Vector3 GridToWorldFrontEdgeMidpoint(GridCoords coords)
		{
			var vertexStart = GridToWorldFrontVertex(coords);
			coords.Column++;
			var vertexEnd = GridToWorldFrontVertex(coords);

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public Vector3 GridToWorldBottomCenter(GridCoords coords)
		{
			var vertexStart = GridToWorldBackEdgeMidpoint(coords);
			var vertexEnd = GridToWorldFrontEdgeMidpoint(coords);

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public Vector3 GridToWorldFrontCenter(GridCoords coords)
		{
			var vertexStart = GridToWorldFrontEdgeMidpoint(coords);
			var vertexEnd = GridToWorldFrontEdgeMidpoint(coords + new GridCoords(1, 0));

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public Vector3 GridToWorldBackCenter(GridCoords coords)
		{
			var vertexStart = GridToWorldBackEdgeMidpoint(coords);
			var vertexEnd = GridToWorldBackEdgeMidpoint(coords + new GridCoords(1, 0));

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public Vector3 GridToWorldCenter(GridCoords coords)
		{
			var vertexStart = GridToWorldFrontCenter(coords);
			var vertexEnd = GridToWorldBackCenter(coords);

			return vertexStart + (vertexEnd - vertexStart) / 2f;
		}

		public IEnumerator ApplyActions(IEnumerable<GridAction> actions)
		{
			if (m_ScoreGrid == null) {
				m_ScoreGrid = new ScoreGrid(Rows, Columns, m_Rules);
			}

			// Count clear hits
			foreach(var clearCoords in actions.OfType<ClearMatchedAction>().SelectMany(a => a.Coords)) {
				this[clearCoords].MatchHits++;
			}

			if (FallTrailEffectsManager) {
				foreach (var action in actions) {
					MoveCellsAction moveAction = action as MoveCellsAction;
					if (moveAction != null) {
						FallTrailEffectsManager.StartFallTrailEffect(moveAction.MovedCells.Select(pair => this[pair.Key].gameObject));
					}
				}
			}

			m_ScoreGrid.ClearLastMatchBonus();
			yield return m_ScoreGrid.ApplyActions(actions);

			GridCoords displayScoreCoord = actions.OfType<ClearMatchedAction>()
				.LastOrDefault()?.Coords
				.SkipLast(1)
				.LastOrDefault()
				?? new GridCoords(-1, -1)
				;

			foreach (var action in MergeActions(actions)) {
				CurrentlyRunningAction = action;

				switch (action) {
					case PlaceAction placeAction:
						yield return PlaceShape(placeAction);
						break;
					case ClearMatchedAction clearAction:
						yield return ClearMatchedCells(clearAction, displayScoreCoord);
						break;
					case MoveCellsAction moveAction:
						yield return MoveCells(moveAction);
						break;
					case PushUpCellsAction pushUpAction:
						yield return PushUpCells(pushUpAction);
						break;
					case EvaluationSequenceFinishAction finishAction:
						ScoreFinished?.Invoke(m_ScoreGrid);
						m_ScoreGrid = null;
						break;
				}
			}

			CurrentlyRunningAction = null;
		}

		private IEnumerable<GridAction> MergeActions(IEnumerable<GridAction> actions)
		{
			List<KeyValuePair<GridCoords, GridCoords>> mergedMoves = new List<KeyValuePair<GridCoords, GridCoords>>();

			foreach (var action in actions) {

				switch (action) {
					// Don't merge so we can score them separately + animations.
					//case ClearMatchedAction clearAction:
					//	clearedBlocks.AddRange(clearAction.Coords);
					//	break;

					case MoveCellsAction moveAction:
						mergedMoves.AddRange(moveAction.MovedCells);
						break;

					default:
						yield return action;
						break;
				}
			}

			// Merge sequences of move actions so animations play together.
			if (mergedMoves.Count > 0) {
				yield return new MoveCellsAction() { MovedCells = mergedMoves.ToArray() };
			}
		}

		private IEnumerator PlaceShape(PlaceAction action)
		{
			Dictionary<int, int> lowestRows = GetLowestRows(action.PlacedShape.ShapeCoords.Select((bind) => {
				GridCoords coords = action.PlaceCoords + bind.Coords;
				coords.WrapColumn(this);
				return coords;
			}));

			foreach (var pair in action.PlacedShape.ShapeCoords) {

				var reusedVisuals = m_PlacedShapeToBeReused?.ShapeCoords
					.Where(sc => sc.Coords == pair.Coords)
					.Select(sc => sc.Value)
					.FirstOrDefault();

				var coords = action.PlaceCoords + pair.Coords;
				coords.WrapColumn(this);

				if (reusedVisuals) {
					Debug.Assert(reusedVisuals.name.Contains(m_BlocksSkinStack.GetPrefabFor(pair.Value).name));
				}

				CreateInstanceAt(coords, pair.Value, reusedVisuals);

				if (lowestRows[coords.Column] == coords.Row) {
					EmitParticlesAt(FallHitEffect, GridToWorldBottomCenter(coords));
				}
			}

			m_PlacedShapeToBeReused = null;

			yield break;
		}

		private IEnumerator ClearMatchedCells(ClearMatchedAction action, GridCoords displayScoreCoord)
		{
			List<GridCoords> coords = action.Coords.ToList();

			// Start from the closer end....
			Vector3 fairyPos = m_Fairy.transform.position;
			if (Vector3.Distance(GridToWorldFrontCenter(coords.First()), fairyPos) > Vector3.Distance(GridToWorldFrontCenter(coords.Last()), fairyPos)) {
				coords.Reverse();
			}

			for(int i = 0; i < coords.Count; ++i) {
				GridCoords coord = coords[i];

				if (m_Fairy) {
					Vector3 frontCenter = GridToWorldFrontCenter(coord);
					// Move a bit forward from the block front face, so light looks good.
					frontCenter += (frontCenter - new Vector3(transform.position.x, frontCenter.y, transform.position.z)).normalized * 0.1f;
					yield return m_Fairy.ChaseTo(frontCenter);

					EmitParticlesAt(MatchBlockEffect, GridToWorldCenter(coord));
				}

				var visualsBlock = this[coord];
				Debug.Assert(visualsBlock != null);
				visualsBlock.MatchHits--;

				if (visualsBlock.MatchHits != 0) {
					visualsBlock.IsHighlighted = true;
				} else {
					DestroyInstanceAt(coord);
				}

				if (displayScoreCoord == coord && m_ScoreGrid != null && m_ScoreGrid.LastMatchBonus.ResultBonusScore > 0) {
					ScoreUpdated?.Invoke(m_ScoreGrid);
				}

				if (m_Fairy == null && i < coords.Count - 1) {
					yield return new WaitForSeconds(MatchBlockDelay);
				}
			}

			if (m_Fairy == null) {
				yield return new WaitForSeconds(MatchActionDelay);
			}
		}

		private IEnumerator MoveCells(MoveCellsAction action)
		{
			if (Application.isPlaying) {

				float startTime = WiseTiming.Time;
				bool waitingBlocks = true;
				while (waitingBlocks) {
					waitingBlocks = false;

					float timePassed = WiseTiming.Time - startTime;

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

			Dictionary<int, int> lowestRows = GetLowestRows(action.MovedCells.Select(pair => pair.Value));

			foreach (var movedPair in action.MovedCells) {
				Debug.Assert(this[movedPair.Key] != null);
				Debug.Assert(this[movedPair.Value] == null);

				if (movedPair.Value.Row >= Rows - GridLevelData.DestroyThreshold) {
					DestroyInstanceAt(movedPair.Key);
					continue;
				}

				ConeVisualsBlock visualsBlock = this[movedPair.Key];
				this[movedPair.Value] = visualsBlock;
				this[movedPair.Key] = null;

				visualsBlock.transform.localScale = GridToScale(movedPair.Value);

				if (lowestRows[movedPair.Value.Column] == movedPair.Value.Row) {
					EmitParticlesAt(FallHitEffect, GridToWorldBottomCenter(movedPair.Value));
				}

				if (visualsBlock.IsHighlighted && movedPair.Value.Row < m_PlayableArea.Row) {
					visualsBlock.IsHighlighted = false;
				} else if (!visualsBlock.IsHighlighted && movedPair.Value.Row >= m_PlayableArea.Row) {
					visualsBlock.IsHighlighted = true;
				}
			}

			yield break;
		}

		private IEnumerator PushUpCells(PushUpCellsAction action)
		{
			if (Application.isPlaying) {

				m_CameraEffects.Shake(action, PushUpWarningShakeRange);

				// Suspens...
				yield return new WaitForSeconds(0.02f);

				ConeVisualsBlock GetBlock(int row, int column, ConeVisualsBlock fallback) => row >= 0 && row < Rows ? this[row, column] : fallback;


				// Each column can be pushed multiple times - do one push per column at a time, until there are no more pushes left.
				// This handles pinned blocks in mid-air.
				var pushesLeft = action.PushBlocks.ToList();
				var pushesDoneCount = action.PushBlocks.GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => 0);
				var risingBlocks = action.PushBlocks.GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => (ConeVisualsBlock)null);
				var columnsProcessedThisFrame = new HashSet<int>();

				float startTime = WiseTiming.Time;
				bool waitingBlocks = true;
				while (waitingBlocks) {
					waitingBlocks = false;

					columnsProcessedThisFrame.Clear();

					float timePassed = WiseTiming.Time - startTime;

					for (int i = 0; i < pushesLeft.Count; i++) {
						int column = pushesLeft[i].Key;

						if (!columnsProcessedThisFrame.Add(column))
							continue;

						ConeVisualsBlock risingBlock = risingBlocks[column];
						if (risingBlock == null) {
							risingBlock = CreateInstanceAt(new GridCoords(-1, column), pushesLeft[i].Value, placeInGrid: false);
							risingBlocks[column] = risingBlock;
						}

						// Pushes are done cell by cell. If same column is pushed twice reflect this in the time needed.
						float distance = pushesDoneCount[column] + 1;
						float timeNeeded = distance / BlockMoveSpeed;

						// -1 is the newly added block, outside the grid.
						int row = -1;
						while (row < Rows - 1 && GetBlock(row, column, risingBlock) != null) {
							var startScale = GridToScale(new GridCoords(row, column));
							var endScale = GridToScale(new GridCoords(row + 1, column));

							// "timePassed / timeNeeded" doesn't work for 2 or more pushes. Treat it as 1 cell distance and use the remainder
							float t = (timePassed / (1 / BlockMoveSpeed)) % 1;

							var scale = Vector3.Lerp(startScale, endScale, t);

							GetBlock(row, column, risingBlock).transform.localScale = scale;

							row++;
						}

						if (timePassed < timeNeeded) {
							waitingBlocks = true;
						} else {
							// Can continue with the next push, so animation doesn't skip a frame.
							columnsProcessedThisFrame.Remove(column);

							// Go up finding the first empty row, as there may be pinned blocks in mid-air.
							// Pinned blocks will be pushed up by other blocks.
							row = 0;
							while (row < Rows - 1 && this[row, column] != null) {
								row++;
							}

							// Check if we're pushing blocks outside the grid (should not happen?).
							var topPlayableRowCoords = new GridCoords(Rows - 1, column);
							if (row == topPlayableRowCoords.Row) {
								if (this[topPlayableRowCoords] != null) {
									Debug.LogError($"Pushing blocks in visuals grid at {column} column, which is full!", this);
									DestroyInstanceAt(new GridCoords(topPlayableRowCoords));
								}
							}

							// Now go down moving up all the rows.
							for (--row; row >= 0; --row) {
								GridCoords toRow = new GridCoords(row + 1, column);

								if (toRow.Row >= Rows - GridLevelData.DestroyThreshold) {
									DestroyInstanceAt(new GridCoords(row, column));
									continue;
								}

								this[toRow] = this[row, column];

								ConeVisualsBlock visualsBlock = this[toRow];
								visualsBlock.transform.localScale = GridToScale(toRow);

								if (visualsBlock.IsHighlighted && toRow.Row < m_PlayableArea.Row) {
									visualsBlock.IsHighlighted = false;
								} else if (!visualsBlock.IsHighlighted && toRow.Row >= m_PlayableArea.Row) {
									visualsBlock.IsHighlighted = true;
								}
							}

							this[0, column] = risingBlock;
							this[0, column].transform.localScale = GridToScale(new GridCoords(0, column));

							risingBlocks[column] = null;
							pushesLeft.RemoveAt(i);
							pushesDoneCount[column]++;
							i--;

							if (pushesLeft.Any(pair => pair.Key == column)) {
								waitingBlocks = true;
							}
						}
					}

					yield return null;
				}

				m_CameraEffects.ClearShake(action);
			}

			yield break;
		}

		private ConeVisualsBlock CreateInstanceAt(GridCoords coords, BlockType blockType, GameObject reuseVisuals = null, bool placeInGrid = true)
		{
			if (reuseVisuals) {
				reuseVisuals.transform.SetParent(transform);
			} else {
				reuseVisuals = GameObject.Instantiate(m_BlocksSkinStack.GetPrefabFor(blockType), transform);

				// Set layer per player so their blocks directional lights don't mix.
				GameLayers.SetLayerRecursively(reuseVisuals, m_BlocksLayer);
			}

			reuseVisuals.transform.position = ConeApex;
			reuseVisuals.transform.localRotation = GridColumnToRotation(coords.Column);

			// Hitting the limit, won't be stored.
			if (coords.Row < Rows) {
				var visualsBlock = reuseVisuals.AddComponent<ConeVisualsBlock>();

				if (placeInGrid) {
					if (this[coords] != null) {
						Debug.LogError($"Creating instance at {coords} which is already taken by {this[coords].name}, for {blockType}", this);
						DestroyInstanceAt(coords);
					}
					this[coords] = visualsBlock;
				}

				reuseVisuals.transform.localScale = GridToScale(coords);

				// Warn that this is outside play area.
				if (coords.Row >= m_PlayableArea.Row) {
					visualsBlock.IsHighlighted = true;
				}

				m_AllBlocks.Add(visualsBlock);

				CreatedVisualsBlock?.Invoke(visualsBlock);

				return visualsBlock;

			} else {
				GameObject.Destroy(reuseVisuals);
				return null;
			}
		}

		private void DestroyInstanceAt(GridCoords coords)
		{
			ConeVisualsBlock block = this[coords];

			DestroyingVisualsBlock?.Invoke(block);

			m_AllBlocks.Remove(block);

			GameObject.Destroy(block.gameObject);
			this[coords] = null;
		}

		private void DestroyInstances()
		{
			for (int row = 0; row < Rows; ++row) {
				for (int column = 0; column < Columns; ++column) {
					if (this[row, column]) {
						DestroyInstanceAt(new GridCoords(row, column));
					}
				}
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

		private void EmitParticlesAt(ParticleSystem particleSystem, Vector3 worldPosition)
		{
			if (particleSystem == null)
				return;

			particleSystem.transform.position = worldPosition;
			Vector3 direction = worldPosition - transform.position;
			direction.y = 0;
			particleSystem.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

			var burst = particleSystem.emission.GetBurst(0);

			int count = (int)burst.count.constant;
			if (burst.count.mode == ParticleSystemCurveMode.TwoConstants) {
				count = (int)Random.Range(burst.count.constantMin, burst.count.constantMax);
			}

			particleSystem.Emit(count);
		}

		// Gather the lowest found rows so we can spawn particles underneath.
		// Dictionary<column, lowest row>
		private Dictionary<int, int> GetLowestRows(IEnumerable<GridCoords> coords)
		{
			Dictionary<int, int> lowestRows = new Dictionary<int, int>();
			foreach (GridCoords coord in coords) {
				int row;
				if (!lowestRows.TryGetValue(coord.Column, out row)) {
					row = int.MaxValue;
				}

				if (coord.Row < row) {
					lowestRows[coord.Column] = coord.Row;
				}
			}

			return lowestRows;
		}


#if UNITY_EDITOR
		[Header("Debug")]
		private GUIStyle m_GizmoCoordsStyle;
		[SerializeField]
		private bool m_GizmoShowGrid = false;
		[SerializeField]
		private bool m_GizmoShowGridHeader = true;
		[SerializeField]
		private bool m_GizmoShowOnlyFaced = true;
		private bool m_GizmoPressed = false;

		private int m_FallingColumn = 0;

		internal void __GizmoUpdateFallingColumn(int fallingColumn)
		{
			m_FallingColumn = fallingColumn;
		}

		void OnDrawGizmos()
		{
			if (Keyboard.current == null)
				return;

			if (Application.isPlaying) {
				// Because Input.GetKeyDown() doesn't work here :(
				if (!m_GizmoPressed && Keyboard.current.gKey.isPressed) {
					m_GizmoShowGrid = !m_GizmoShowGrid;
					m_GizmoPressed = true;
				}
				if (!m_GizmoPressed && Keyboard.current.hKey.isPressed) {
					m_GizmoShowGridHeader = !m_GizmoShowGridHeader;
					m_GizmoPressed = true;
				}
				if (!m_GizmoPressed && Keyboard.current.fKey.isPressed) {
					m_GizmoShowOnlyFaced = !m_GizmoShowOnlyFaced;
					m_GizmoPressed = true;
				}
				if (m_GizmoPressed && !Keyboard.current.gKey.isPressed && !Keyboard.current.hKey.isPressed && !Keyboard.current.fKey.isPressed) {
					m_GizmoPressed = false;
				}
			}

			if (m_GizmoCoordsStyle == null || true) {
				m_GizmoCoordsStyle = new GUIStyle(GUI.skin.label);
				m_GizmoCoordsStyle.alignment = TextAnchor.MiddleCenter;
				m_GizmoCoordsStyle.padding = new RectOffset();
				m_GizmoCoordsStyle.margin = new RectOffset();
				m_GizmoCoordsStyle.contentOffset = new Vector2(-6, -5);
				m_GizmoCoordsStyle.normal.textColor = new Color(0f, 1f, 0f, 0.6f);
				m_GizmoCoordsStyle.fontSize = 14;
				m_GizmoCoordsStyle.fontStyle = FontStyle.Bold;
			}

			int rows = m_Blocks != null ? Rows : Mathf.RoundToInt(ConeHeight / BlockHeight);
			int columns = m_Blocks != null ? Columns : 13;

			if (Mathf.Approximately(ConeApex.magnitude, 0f) || !Application.isPlaying) {
				CalculateCone(columns);
			}

			var coords = new GridCoords();

			if (m_GizmoShowGridHeader) {

				coords.Row = 0;
				for (coords.Column = 0; coords.Column < columns; ++coords.Column) {

					if (m_GizmoShowOnlyFaced && Application.isPlaying && Mathf.Abs(m_FallingColumn - coords.Column) % 11 > 2)
						continue;

					var position = GridToWorldFrontEdgeMidpoint(coords);
					position.y -= 1f;

					UnityEditor.Handles.Label(position, coords.Column.ToString(), m_GizmoCoordsStyle);
				}
			}

			if (m_GizmoShowGrid) {
				for (coords.Row = 0; coords.Row < rows; ++coords.Row) {
					for (coords.Column = 0; coords.Column < columns; ++coords.Column) {

						if (m_GizmoShowOnlyFaced && Application.isPlaying && Mathf.Abs(m_FallingColumn - coords.Column) % 11 > 2)
							continue;

						var vertexStart = GridToWorldFrontVertex(coords);
						var vertexEndNextColumn = GridToWorldFrontVertex(new GridCoords(coords.Row, coords.Column + 1));
						var vertexEndNextRow = GridToWorldFrontVertex(new GridCoords(coords.Row + 1, coords.Column));

						UnityEditor.Handles.color = new Color(0, 0, 0, 0.4f);
						UnityEditor.Handles.DrawLine(vertexStart, vertexEndNextColumn);
						UnityEditor.Handles.DrawLine(vertexStart, vertexEndNextRow);

					}
				}
			}
		}
#endif
	}

}
