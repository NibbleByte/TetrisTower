using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[CreateAssetMenu(fileName = "UnknownPlaythroughTemplate", menuName = "Tetris Tower/Playthrough Template")]
	public class SeqPlaythroughTemplate : PlaythroughTemplateBase
	{
		[SerializeField] private SeqPlaythroughData m_PlayerData;

		public override bool HasActiveLevel => m_PlayerData.TowerLevel != null;

		public override IPlaythroughData GeneratePlaythroughData(GameConfig config)
		{
			m_PlayerData.Validate(config.AssetsRepository, this);

			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(m_PlayerData, config.Converters);

			return Newtonsoft.Json.JsonConvert.DeserializeObject<SeqPlaythroughData>(serialized, config.Converters);
		}

		public override IEnumerable<LevelParamData> GetAllLevels()
		{
			return m_PlayerData.Levels;
		}


#if UNITY_EDITOR

		private void OnValidate()
		{
			if (UnityEditor.EditorApplication.isUpdating)
				return;

			if (m_PlayerData != null) {
				GameConfig gameConfig = GameConfig.FindDefaultConfig();
				m_PlayerData.Validate(gameConfig.AssetsRepository, this);
			}
		}

		// Just modify the hard-coded array. Copy one from the unit tests.
		[ContextMenu("Setup From Array")]
		void SetupFromArray()
		{
			#region Setup Blocks

#pragma warning disable CS0219 // Unused variables. Really don't care - this is a debug code.

			BlockType R = BlockType.B1; // "Red" block
			BlockType G = BlockType.B2; // "Green" block
			BlockType B = BlockType.B3; // "Blue" block

			BlockType W = BlockType.SpecialWildBlock;     // "Wild" block
			BlockType Q = BlockType.SpecialBlockSmite;    // "Block Smite"
			BlockType H = BlockType.SpecialRowSmite;      // "Row Smite"

			BlockType S = BlockType.StaticBlock;          // "Static" block

			BlockType N = BlockType.WonBonusBlock;        // "null" block

#pragma warning restore CS0219

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

					if (blockType == BlockType.None)
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
			BlockSkin[] blocksPool = m_PlayerData.TowerLevel.NormalBlocksSkins;
			if (blocksPool == null || blocksPool.Length == 0) {
				GameConfig gameConfig = GameConfig.FindDefaultConfig();

				if (!gameConfig) {
					Debug.LogError("Specify pool of blocks or have at least one GameConfig in the project.", this);
					return;
				}

				blocksPool = gameConfig.DefaultBlocksSet.BlockSkins.ToArray();
			}

			for (int row = 0; row < m_PlayerData.TowerLevel.Grid.Rows; ++row) {
				for(int column = 0; column < m_PlayerData.TowerLevel.Grid.Columns; ++column) {

					var blockType = row < m_PlayerData.TowerLevel.Grid.Rows - 3
						? blocksPool[(column + row % 2) % blocksPool.Length].BlockType
						: BlockType.None
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