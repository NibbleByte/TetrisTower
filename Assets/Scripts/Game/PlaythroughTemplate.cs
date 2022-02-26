using System.Collections;
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
		[ContextMenu("Stack up!")]
		void StackUp()
		{
			for(int row = 0; row < m_PlayerData.TowerLevel.Grid.Rows; ++row) {
				for(int column = 0; column < m_PlayerData.TowerLevel.Grid.Columns; ++column) {

					var blockType = row < m_PlayerData.TowerLevel.Grid.Rows - 5
						? m_PlayerData.TowerLevel.BlocksPool[(column + row % 2) % m_PlayerData.TowerLevel.BlocksPool.Length]
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