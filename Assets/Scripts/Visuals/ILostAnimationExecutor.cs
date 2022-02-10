using System.Collections;
using System.Collections.Generic;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Behaviours that could be selected to display lost animation sequence.
	/// The tower level prefab should have default behaviours setup, but the scene can have specific ones as well.
	/// </summary>
	public interface ILostAnimationExecutor
	{
		public int Chance { get; }

		public IEnumerator Execute(ConeVisualsGrid visualsGrid, Transform fallingVisualsContainer, List<KeyValuePair<GridCoords, ConeVisualsBlock>> blocks);

		public void Interrupt();
	}
}
