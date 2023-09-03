using UnityEngine;
using System;

namespace TetrisTower.Visuals
{
	public enum VisualsBlockState
	{
		Normal,
		Highlighted,
		FallTrailEffect,
	}

	public class ConeVisualsBlock : MonoBehaviour
	{

		[NonSerialized]
		public int MatchHits = 0;

		public VisualsBlockState State { get; private set; }

		public bool IsHighlighted => State == VisualsBlockState.Highlighted;

		private Renderer m_Renderer;
		private Material m_OriginalMaterial;

		private MaterialPropertyBlock m_AppliedBlock;
		private Material m_AppliedMaterial;

		private int m_HighlightEnabledId;

		void Awake()
		{
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

		public ConeVisualsBlock SetFloat(string propertyName, float value)
		{
			if (m_AppliedBlock == null) {
				Debug.LogError($"No material property block to apply float {propertyName} {value} to.", this);
				return this;
			}

			m_AppliedBlock.SetFloat(propertyName, value);

			return this;
		}

		public ConeVisualsBlock SetFloat(int propertyNameId, float value)
		{
			if (m_AppliedBlock == null) {
				Debug.LogError($"No material property block to apply float {propertyNameId} {value} to.", this);
				return this;
			}

			m_AppliedBlock.SetFloat(propertyNameId, value);

			return this;
		}

		public void ReapplyMaterialPropertyBlock()
		{
			if (m_AppliedBlock == null) {
				Debug.LogError("No material property block to reapply.", this);
				return;
			}

			m_Renderer.SetPropertyBlock(m_AppliedBlock);
		}


		private void ClearMaterialPropertyBlock()
		{
			m_AppliedBlock = null;
			m_Renderer.SetPropertyBlock(null);	// Setting it to null clears the used block and allows batching.
		}


		public void RestoreToNormal()
		{
			ClearMaterialPropertyBlock(); // So it can be SRP Batched again.

			m_Renderer.sharedMaterial = m_OriginalMaterial;

			if (m_AppliedMaterial) {
				Destroy(m_AppliedMaterial);
				m_AppliedMaterial = null;
			}

			State = VisualsBlockState.Normal;
		}

		public void RestoreToNormal(VisualsBlockState expectedState)
		{
			if (State != expectedState && expectedState != VisualsBlockState.Normal) {
				Debug.LogWarning($"{nameof(ConeVisualsBlock)}.{nameof(RestoreToNormal)}() expected {expectedState}, but found {State}, on {name}", this);
			}

			RestoreToNormal();
		}

		public void SetHighlight()
		{
			if (State == VisualsBlockState.Highlighted)
				return;

			if (State != VisualsBlockState.Normal) {
				Debug.LogWarning($"Failed changing state to {VisualsBlockState.Highlighted} from {State}, on {name}", this);
				return;
			}

			if (m_AppliedBlock != null) {
				Debug.LogWarning($"Failed changing state to {VisualsBlockState.Highlighted} as there is a materials property block in use, on {name}", this);
				return;
			}

			State = VisualsBlockState.Highlighted;
			
			m_AppliedBlock = new MaterialPropertyBlock();
			m_AppliedBlock.SetFloat(m_HighlightEnabledId, 1f);

			ReapplyMaterialPropertyBlock();
		}

		public ConeVisualsBlock StartFallTrailEffect(Shader fallTrailEffectShader)
		{
			if (State == VisualsBlockState.FallTrailEffect)
				return this;

			if (State != VisualsBlockState.Normal) {
				Debug.LogWarning($"Failed changing state to {VisualsBlockState.FallTrailEffect} from {State}, on {name}", this);
				return this;
			}

			if (m_AppliedBlock != null) {
				Debug.LogWarning($"Failed changing state to {VisualsBlockState.FallTrailEffect} as there is a materials property block in use, on {name}", this);
				return this;
			}

			State = VisualsBlockState.FallTrailEffect;


			m_AppliedMaterial = new Material(m_OriginalMaterial);
			m_AppliedMaterial.name += $"_{fallTrailEffectShader.name}";
			m_AppliedMaterial.shader = fallTrailEffectShader;

			m_Renderer.sharedMaterial = m_AppliedMaterial;

			m_AppliedBlock = new MaterialPropertyBlock();

			return this;
		}
	}
}