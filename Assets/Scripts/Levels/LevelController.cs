using System.Collections;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Levels
{
	public class LevelController : MonoBehaviour
	{
		public LevelData LevelData { get; private set; }
		// For debug to be displayed by the Inspector!
		[SerializeReference] private LevelData m_DebugLevelData;


		public List<GameGrid> Grids { get; private set; } = new List<GameGrid>();

		public event System.Action LevelInitialized;
		public event System.Action LevelFallingShapeChanged;

		public void Init(LevelData data)
		{
			LevelData = m_DebugLevelData = data;

			Grids.Clear();
			Grids.Add(LevelData.Grid);

			LevelInitialized?.Invoke();
			LevelFallingShapeChanged?.Invoke();
		}

		public IEnumerator RunActions(IEnumerable<GridAction> actions)
		{
			foreach(var grid in Grids) {

				var transformedActions = actions;
				if (grid is GridActionsTransformer transformer) {
					transformedActions = transformer.Transform(actions);
				}

				foreach(var action in transformedActions) {
					yield return action.Apply(grid);
				}
			}
		}
	}
}