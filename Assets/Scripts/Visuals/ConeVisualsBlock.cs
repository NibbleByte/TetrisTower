using UnityEngine;
using System;
using System.Collections.Generic;

namespace TetrisTower.Visuals
{
	public class ConeVisualsBlock : MonoBehaviour
	{
		[NonSerialized]
		public int MatchHits = 0;

		private Renderer m_Renderer;
		private Material m_OriginalMaterial;

		private MaterialPropertyBlock m_AppliedBlock;
		private Material m_AppliedMaterial;

		private Dictionary<int, float> m_FloatProperties = new ();
		private Dictionary<int, Vector4> m_VectorProperties = new ();
		private Dictionary<int, Texture> m_TextureProperties = new ();

		private int m_HighlightEnabledId;

		void Awake()
		{
			m_AppliedBlock = new MaterialPropertyBlock();
			m_Renderer = GetComponentInChildren<Renderer>();
			m_OriginalMaterial = m_Renderer.sharedMaterial;

			m_HighlightEnabledId = Shader.PropertyToID("_HighlightEnabled");
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
			m_TextureProperties.Clear();

			ApplyProperties();
		}

		public void ApplyProperties()
		{
			if (0 == m_FloatProperties.Count + m_VectorProperties.Count + m_TextureProperties.Count) {
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

		private bool m_IsHighlighted;
		public bool IsHighlighted {
			get => m_IsHighlighted;
			set {
				if (value) {
					SetFloat(m_HighlightEnabledId, 1f);
				} else {
					ClearProperty(m_HighlightEnabledId);
				}
				ApplyProperties();

				m_IsHighlighted = value;
			}
		}

	}
}