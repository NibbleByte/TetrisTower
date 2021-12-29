using UnityEngine;
using System;
using System.Collections.Generic;

namespace TetrisTower.Visuals
{
	public class ConeVisualsTester : MonoBehaviour
	{
		private List<ConeVisualsBlock> m_Blocks = new List<ConeVisualsBlock>();

		private void Awake()
		{
			foreach(var renderer in GetComponentsInChildren<Renderer>()) {
				m_Blocks.Add(renderer.gameObject.AddComponent<ConeVisualsBlock>());
			}
		}

		public void HighlightBlocks()
		{
			foreach(var block in m_Blocks) {
				block.Highlight();
			}
		}

		public void ClearBlockHighlights()
		{
			foreach(var block in m_Blocks) {
				block.ClearHighlight();
			}
		}
	}
}