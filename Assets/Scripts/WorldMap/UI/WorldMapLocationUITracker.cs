using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.WorldMap.UI
{
	public class WorldMapLocationUITracker : MonoBehaviour
	{
		private WorldMapLevelParamData m_LevelData;

		public Image LocationImage;

		public WorldLocationState State { get; private set; }

		public void Setup(WorldMapLevelParamData levelData)
		{
			m_LevelData = levelData;

			LocationImage.sprite = m_LevelData.Thumbnail;
		}

		public void SetState(WorldLocationState state)
		{
			State = state;

			switch (State) {
				case WorldLocationState.Hidden:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Unlocked:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Revealed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				case WorldLocationState.Completed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				default: throw new System.NotSupportedException(State.ToString());
			}
		}
	}
}