using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class ConeVisualsMaterialsDatabase : MonoBehaviour
	{
		public Shader HighlightShader;
		public Shader FallTrailEffectShader;

		private class MaterialVariations
		{
			public Material Highlight;
			public Material FallTrailEffect;
		}

		private Dictionary<Material, MaterialVariations> m_MaterialsCache = new Dictionary<Material, MaterialVariations>();

		public Material GetHighlightSharedMaterial(Material original)
		{
			return GetVariations(original).Highlight;
		}

		public Material GetFallTrailEffectSharedMaterial(Material original)
		{
			return GetVariations(original).FallTrailEffect;
		}

		private MaterialVariations GetVariations(Material originalMaterial)
		{
			if (m_MaterialsCache.TryGetValue(originalMaterial, out MaterialVariations variations))
				return variations;

			variations = new MaterialVariations();

			// We assume that different shaders have the same parameters so no additional setup would be needed.
			variations.Highlight = new Material(originalMaterial);
			variations.Highlight.name += "_Highlight";
			variations.Highlight.shader = HighlightShader;

			variations.FallTrailEffect = new Material(originalMaterial);
			variations.FallTrailEffect.name += "_FallTrailEffect";
			variations.FallTrailEffect.shader = FallTrailEffectShader;

			m_MaterialsCache[originalMaterial] = variations;

			return variations;
		}

		private void OnDestroy()
		{
			foreach(MaterialVariations mats in m_MaterialsCache.Values) {
				Destroy(mats.Highlight);
				Destroy(mats.FallTrailEffect);
			}

			m_MaterialsCache.Clear();
		}
	}
}