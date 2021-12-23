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
			var db = GetComponentInParent<ConeVisualsMaterialsDatabase>();
			var renderer = GetComponentInChildren<Renderer>();

			renderer.sharedMaterial = db.GetHighlightSharedMaterial(renderer.sharedMaterial);
		}
	}
}