using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapLocation : MonoBehaviour
	{
		public string LevelID => m_LevelData.LevelID;
		private WorldMapLevelParamData m_LevelData;

		public void Setup(WorldMapLevelParamData levelData)
		{
			m_LevelData = levelData;
		}

		public WorldLocationState State { get; private set; }

		public void SetState(WorldLocationState state)
		{
			State = state;
			gameObject.SetActive(true);

			// At the moment 3D location markers are always visible.
		}
	}
}