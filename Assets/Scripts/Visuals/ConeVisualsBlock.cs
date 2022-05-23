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

		private Effects.ConeVisualsMaterialsDatabase m_MaterialsDatabase;
		private Renderer m_Renderer;
		private Material m_OriginalMaterial;


		void Awake()
		{
			m_MaterialsDatabase = GetComponentInParent<Effects.ConeVisualsMaterialsDatabase>();
			m_Renderer = GetComponentInChildren<Renderer>();
			m_OriginalMaterial = m_Renderer.sharedMaterial;
		}


		public void RestoreToNormal()
		{
			m_Renderer.sharedMaterial = m_OriginalMaterial;

			m_Renderer.SetPropertyBlock(null);	// So it can be SRP Batched again.

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

			m_Renderer.sharedMaterial = m_MaterialsDatabase.GetHighlightSharedMaterial(m_OriginalMaterial);

			State = VisualsBlockState.Highlighted;
		}

		public void SetFallTrailEffect()
		{
			if (State == VisualsBlockState.FallTrailEffect)
				return;

			if (State != VisualsBlockState.Normal) {
				Debug.LogWarning($"Failed changing state to {VisualsBlockState.FallTrailEffect} from {State}, on {name}", this);
				return;
			}

			m_Renderer.sharedMaterial = m_MaterialsDatabase.GetFallTrailEffectSharedMaterial(m_OriginalMaterial);

			State = VisualsBlockState.FallTrailEffect;
		}

		public void SetMaterialPropertyBlock(MaterialPropertyBlock block)
		{
			if (State == VisualsBlockState.Normal) {
				Debug.LogWarning($"Trying to set material property block while state is normal on block {name}", this);
				return;
			}

			m_Renderer.SetPropertyBlock(block);
		}
	}
}