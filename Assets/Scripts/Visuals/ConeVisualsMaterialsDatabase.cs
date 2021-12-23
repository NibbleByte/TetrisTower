using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Visuals
{
	public class ConeVisualsMaterialsDatabase : MonoBehaviour
	{
		public Shader HighlightShader;

		private Dictionary<Material, Material> m_HighlightMaterials = new Dictionary<Material, Material>();

		public Material GetHighlightSharedMaterial(Material original)
		{
			Material highlight;
			if (m_HighlightMaterials.TryGetValue(original, out highlight))
				return highlight;

			// We assume that different shaders have the same parameters so no additional setup would be needed.
			highlight = new Material(original);
			highlight.shader = HighlightShader;

			m_HighlightMaterials[original] = highlight;

			return highlight;
		}

		private void OnDestroy()
		{
			foreach(Material material in m_HighlightMaterials.Values) {
				Destroy(material);
			}

			m_HighlightMaterials.Clear();
		}
	}
}