using UnityEngine;
using System;
using System.Collections.Generic;

namespace TetrisTower.Visuals
{
	public enum VisualsBlockHighlightType
	{
		None,

		MatchCombo = 1 << 0,
		Objective = 1 << 2,
		Danger = 1 << 4,
	}

	public class ConeVisualsBlock : MonoBehaviour
	{
		[NonSerialized]
		public int MatchHits = 0;

		public BlockVisualsSharedSettings SharedSettings { private get; set; }


		private VisualsBlockHighlightType m_Highlight = VisualsBlockHighlightType.None;

		public bool IsHighlighted(VisualsBlockHighlightType type) => m_Highlight.HasFlag(type);
		public void SetHighlighted(VisualsBlockHighlightType type, bool value)
		{
			if (m_Highlight.HasFlag(type) == value)
				return;

			m_Highlight = value ? m_Highlight | type : m_Highlight ^ type;

			// Highlights have priority.
			if (m_Highlight.HasFlag(VisualsBlockHighlightType.Danger)) {
				SetColor(m_HighlightColorId, SharedSettings.DangerColor);
			} else if (m_Highlight.HasFlag(VisualsBlockHighlightType.Objective)) {
				SetColor(m_HighlightColorId, SharedSettings.ObjectiveColor);
			} else if (m_Highlight.HasFlag(VisualsBlockHighlightType.MatchCombo)) {
				SetColor(m_HighlightColorId, SharedSettings.MatchComboColor);
			} else {
				ClearProperty(m_HighlightColorId);
			}

			ApplyProperties();
		}

		public void ClearAnyHighlights()
		{
			if (m_Highlight == VisualsBlockHighlightType.None)
				return;

			m_Highlight = VisualsBlockHighlightType.None;
			ClearProperty(m_HighlightColorId);

			ApplyProperties();
		}

		private Renderer m_Renderer;
		private Material m_OriginalMaterial;

		private MaterialPropertyBlock m_AppliedBlock;
		private Material m_AppliedMaterial;

		private Dictionary<int, float> m_FloatProperties = new ();
		private Dictionary<int, Vector4> m_VectorProperties = new ();
		private Dictionary<int, Color> m_ColorProperties = new ();
		private Dictionary<int, Texture> m_TextureProperties = new ();

		private int m_HighlightColorId;

		void Awake()
		{
			m_AppliedBlock = new MaterialPropertyBlock();
			m_Renderer = GetComponentInChildren<Renderer>();
			m_OriginalMaterial = m_Renderer.sharedMaterial;

			m_HighlightColorId = Shader.PropertyToID("_Highlight_Color");
		}

		void OnDestroy()
		{
			if (m_AppliedMaterial) {
				Destroy(m_AppliedMaterial);
			}
		}

		#region Material Properties

		public float GetFloat(int propertyNameId)
		{
			float value;
			m_FloatProperties.TryGetValue(propertyNameId, out value);
			return value;
		}

		public ConeVisualsBlock SetFloat(int propertyNameId, float value)
		{
			m_FloatProperties[propertyNameId] = value;

			m_AppliedBlock.SetFloat(propertyNameId, value);

			return this;
		}

		public Vector4 GetVector(int propertyNameId)
		{
			Vector4 value;
			m_VectorProperties.TryGetValue(propertyNameId, out value);
			return value;
		}

		public ConeVisualsBlock SetVector(int propertyNameId, Vector4 value)
		{
			m_VectorProperties[propertyNameId] = value;

			m_AppliedBlock.SetVector(propertyNameId, value);

			return this;
		}

		public Color GetColor(int propertyNameId)
		{
			Color value;
			m_ColorProperties.TryGetValue(propertyNameId, out value);
			return value;
		}

		public ConeVisualsBlock SetColor(int propertyNameId, Color value)
		{
			m_ColorProperties[propertyNameId] = value;

			m_AppliedBlock.SetColor(propertyNameId, value);

			return this;
		}


		public Texture GetTexture(int propertyNameId)
		{
			Texture value;
			m_TextureProperties.TryGetValue(propertyNameId, out value);
			return value;
		}

		public ConeVisualsBlock SetTexture(int propertyNameId, Texture texture)
		{
			m_TextureProperties[propertyNameId] = texture;

			m_AppliedBlock.SetTexture(propertyNameId, texture);

			return this;
		}

		public ConeVisualsBlock ClearProperty(int propertyNameId) => ClearProperties(propertyNameId);

		public ConeVisualsBlock ClearProperties(params int[] propertyNameIds)
		{
			bool removed = false;

			foreach(int propertyNameId in propertyNameIds) {
				removed = m_FloatProperties.Remove(propertyNameId) | removed;
				removed = m_VectorProperties.Remove(propertyNameId) | removed;
				removed = m_ColorProperties.Remove(propertyNameId) | removed;
				removed = m_TextureProperties.Remove(propertyNameId) | removed;
			}


			if (removed) {
				m_AppliedBlock.Clear();

				foreach (var pair in m_FloatProperties) {
					m_AppliedBlock.SetFloat(pair.Key, pair.Value);
				}

				foreach (var pair in m_VectorProperties) {
					m_AppliedBlock.SetVector(pair.Key, pair.Value);
				}

				foreach (var pair in m_ColorProperties) {
					m_AppliedBlock.SetColor(pair.Key, pair.Value);
				}

				foreach (var pair in m_TextureProperties) {
					m_AppliedBlock.SetTexture(pair.Key, pair.Value);
				}
			}

			return this;
		}

		public void ClearAllProperties()
		{
			m_AppliedBlock.Clear();

			m_FloatProperties.Clear();
			m_VectorProperties.Clear();
			m_ColorProperties.Clear();
			m_TextureProperties.Clear();

			ApplyProperties();
		}

		public void ApplyProperties()
		{
			if (0 == m_FloatProperties.Count + m_VectorProperties.Count + m_ColorProperties.Count + m_TextureProperties.Count) {
				m_Renderer.SetPropertyBlock(null);
			} else {
				m_Renderer.SetPropertyBlock(m_AppliedBlock);
			}
		}

		#endregion

		#region Shader Override

		public ConeVisualsBlock OverrideShader(Shader shader)
		{
			if (m_AppliedMaterial) {
				Destroy(m_AppliedMaterial);
			}

			m_AppliedMaterial = new Material(m_OriginalMaterial);
			m_AppliedMaterial.name += $"_{shader.name}";
			m_AppliedMaterial.shader = shader;

			m_Renderer.sharedMaterial = m_AppliedMaterial;

			return this;
		}

		public void ClearShaderOverride()
		{
			// NOTE: calling this will leak the properties for this shader that are set by the user.
			//		 User should clear those up if needed.

			m_Renderer.sharedMaterial = m_OriginalMaterial;

			if (m_AppliedMaterial) {
				Destroy(m_AppliedMaterial);
				m_AppliedMaterial = null;
			}
		}

		#endregion
	}
}