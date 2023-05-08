using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapLocation : MonoBehaviour
	{
		public string LevelID;

		public WorldLocationState State { get; private set; }

		public void SetState(WorldLocationState state)
		{
			State = state;

			switch(State) {
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
					transform.localScale = Vector3.one / 2f;
					gameObject.SetActive(true);
					break;

				default: throw new System.NotSupportedException(State.ToString());
			}
		}
	}
}