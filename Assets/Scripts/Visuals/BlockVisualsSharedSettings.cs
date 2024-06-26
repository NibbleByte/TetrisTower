using UnityEngine;

namespace TetrisTower.Visuals
{
	[CreateAssetMenu(fileName = "UnknownBlockVisualsSharedSettings", menuName = "Tetris Tower/Block Visuals Shared Settings")]
	public class BlockVisualsSharedSettings : ScriptableObject
	{
		public Color MatchComboColor = Color.yellow;
		public Color ObjectiveColor = new Color(0.1048387f, 0.4717741f, 0.5f, 1f);
		public Color DangerColor = Color.red;
	}
}