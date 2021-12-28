using UnityEngine;
using System;

namespace TetrisTower.Visuals
{
	public class ConeVisualsBlock : MonoBehaviour
	{
		[NonSerialized]
		public int MatchHits = 0;

		public bool IsHighlighted { get; private set; }

		public void Highlight()
		{
			if (IsHighlighted)
				return;

			var db = GetComponentInParent<ConeVisualsMaterialsDatabase>();
			var renderer = GetComponentInChildren<Renderer>();

			renderer.sharedMaterial = db.GetHighlightSharedMaterial(renderer.sharedMaterial);

			IsHighlighted = true;
		}

		public void ClearHighlight()
		{
			if (!IsHighlighted)
				return;

			var db = GetComponentInParent<ConeVisualsMaterialsDatabase>();
			var renderer = GetComponentInChildren<Renderer>();

			renderer.sharedMaterial = db.GetHighlightOriginalMaterial(renderer.sharedMaterial);

			IsHighlighted = false;
		}
	}
}