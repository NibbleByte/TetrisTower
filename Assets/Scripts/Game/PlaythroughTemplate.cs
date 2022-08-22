using System.Collections;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownPlaythroughTemplate", menuName = "Tetris Tower/Playthrough Template")]
	public class PlaythroughTemplate : ScriptableObject
	{
		[SerializeField] private PlaythroughData m_PlayerData;

		public PlaythroughData GeneratePlaythroughData(GameConfig config)
		{
			m_PlayerData.Validate(config.AssetsRepository, this);

			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(m_PlayerData, config.Converters);

			return Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(serialized, config.Converters);
		}


#if UNITY_EDITOR
		// Just modify the hard-coded array. Copy one from the unit tests.
		[ContextMenu("Setup From Array")]
		void SetupFromArray()
		{
			#region Setup Blocks

			BlockType R; // "Red" block
			BlockType G; // "Green" block
			BlockType B; // "Blue" block

			BlockType W; // "Wild" block
			BlockType Q; // "Block Smite"
			BlockType H; // "Row Smite"

			BlockType S; // "Static" block

			BlockType N; // "null" block

			GameConfig gameConfig = GameConfig.FindDefaultConfig();

			R = gameConfig.DefaultBlocksSet.Blocks[0];
			G = gameConfig.DefaultBlocksSet.Blocks[1];
			B = gameConfig.DefaultBlocksSet.Blocks[2];

			W = gameConfig.DefaultBlocksSet.WildBlock;
			Q = gameConfig.DefaultBlocksSet.BlockSmite;
			H = gameConfig.DefaultBlocksSet.RowSmite;

			S = gameConfig.DefaultBlocksSet.WonBonusBlock;

			N = null;

			#endregion

			BlockType[,] blocks = new BlockType[,] {
				{ R, R, R, W, B, B, N, N, N, N, N, N, N },
				{ R, R, R, W, B, N, N, N, N, N, N, N, N },
				{ S, S, S, N, S, S, S, N, N, N, N, N, N },
				{ R, R, W, B, B, N, N, N, W, N, N, N, N },
				{ R, R, W, B, N, N, N, N, S, S, N, N, N },
				{ S, S, S, N, S, S, S, N, W, W, N, N, N },
				{ R, W, R, N, B, W, B, N, S, S, S, N, N },
				{ S, S, S, N, S, S, S, N, W, W, W, N, N },
				{ R, W, W, N, W, W, B, N, S, S, S, S, N },
				{ R, R, W, N, W, B, B, N, W, W, W, W, N },
			};



			#region Apply to Grid

			m_PlayerData.TowerLevel.FallDistanceNormalized = 0;
			m_PlayerData.TowerLevel.Grid = new BlocksGrid(blocks.GetLength(0), blocks.GetLength(1));

			for (int row = 0; row < m_PlayerData.TowerLevel.Grid.Rows; ++row) {
				for (int column = 0; column < m_PlayerData.TowerLevel.Grid.Columns; ++column) {

					BlockType blockType = blocks[row, column];

					if (blockType == null)
						continue;

					var coords = new GridCoords(m_PlayerData.TowerLevel.Grid.Rows - 1 - row, column);
					var placedShape = new BlocksShape() { ShapeCoords = new GridShape<BlockType>.ShapeBind[] { BlocksShape.Bind(new GridCoords(), blockType) } };
					var action = new PlaceAction() { PlaceCoords = coords, PlacedShape = placedShape };

					var enumerator = m_PlayerData.TowerLevel.Grid.ApplyActions(new PlaceAction[] { action });
					while (enumerator.MoveNext()) {
						var enumerator2 = enumerator.Current as IEnumerator;
						if (enumerator2 != null) {
							while (enumerator2.MoveNext()) { }
						}
					};
				}
			}

			#endregion
		}

		[ContextMenu("Stack up!")]
		void StackUp()
		{
			BlockType[] blocksPool = m_PlayerData.TowerLevel.BlocksPool;
			if (blocksPool == null || blocksPool.Length == 0) {
				GameConfig gameConfig = GameConfig.FindDefaultConfig();

				if (!gameConfig) {
					Debug.LogError("Specify pool of blocks or have at least one GameConfig in the project.", this);
					return;
				}

				blocksPool = gameConfig.DefaultBlocksSet.Blocks.ToArray();
			}

			for (int row = 0; row < m_PlayerData.TowerLevel.Grid.Rows; ++row) {
				for(int column = 0; column < m_PlayerData.TowerLevel.Grid.Columns; ++column) {

					var blockType = row < m_PlayerData.TowerLevel.Grid.Rows - 3
						? blocksPool[(column + row % 2) % blocksPool.Length]
						: null
						;

					var coords = new GridCoords(row, column);
					var placedShape = new BlocksShape() { ShapeCoords = new GridShape<BlockType>.ShapeBind[] { BlocksShape.Bind(new GridCoords(), blockType) } };
					var action = new PlaceAction() { PlaceCoords = coords, PlacedShape = placedShape };

					var enumerator = m_PlayerData.TowerLevel.Grid.ApplyActions(new PlaceAction[] { action });
					while (enumerator.MoveNext()) {
						var enumerator2 = enumerator.Current as IEnumerator;
						if (enumerator2 != null) {
							while (enumerator2.MoveNext()) { }
						}
					};
				}
			}

			UnityEditor.EditorUtility.SetDirty(this);
		}
#endif
	}
}