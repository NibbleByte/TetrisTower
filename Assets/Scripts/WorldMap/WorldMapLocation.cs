using UnityEngine;

namespace TetrisTower.WorldMap
{
	public class WorldMapLocation : MonoBehaviour
	{
		public enum VisibleState
		{
			Hidden,
			Revealed,
			Completed
		}

		public string LevelID;

		public VisibleState State { get; private set; }

		public void SetState(VisibleState state)
		{
			State = state;

			switch(State) {
				case VisibleState.Hidden:
					gameObject.SetActive(false);
					break;

				case VisibleState.Revealed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				case VisibleState.Completed:
					// TODO: Colorize differently
					transform.localScale = Vector3.one / 2f;
					gameObject.SetActive(true);
					break;

				default: throw new System.NotSupportedException(State.ToString());
			}
		}
	}
}