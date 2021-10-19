using UnityEngine;
using System;

namespace TetrisTower.Visuals
{
	public class ConeVisualsBlock : MonoBehaviour
	{
		[NonSerialized]
		public int MatchHits = 0;

		public void HighlightHit()
		{
			transform.Translate(Vector3.forward * 0.5f);
		}
	}
}