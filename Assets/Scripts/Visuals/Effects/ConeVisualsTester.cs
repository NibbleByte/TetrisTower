using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;

namespace TetrisTower.Visuals.Effects
{
	public class ConeVisualsTester : MonoBehaviour
	{
#if UNITY_EDITOR
		public float FallSpeed = 0.02f;

		public GameObject[] FallTrailEffectPreviewBlocks;

		public BlockVisualsSharedSettings BlockVisualsSettings;

		private List<ConeVisualsBlock> m_Blocks = new List<ConeVisualsBlock>();
		private FallTrailEffectsManager m_FallTrailEffectManager;

		private float m_FallTrailEffect_StartTime;
		private List<KeyValuePair<ConeVisualsBlock, Vector3>> m_FallTrailEffect_PreviewBlocks = new List<KeyValuePair<ConeVisualsBlock, Vector3>>();

		private void Awake()
		{
			foreach(var renderer in GetComponentsInChildren<Renderer>()) {
				m_Blocks.Add(renderer.gameObject.AddComponent<ConeVisualsBlock>());
				m_Blocks.Last().SharedSettings = BlockVisualsSettings;
			}

			m_FallTrailEffectManager = GetComponentInParent<FallTrailEffectsManager>();
			m_FallTrailEffectManager.BlocksLayer = GameLayers.BlocksLayer(0);
		}

		public void HighlightMatchCombo() => HighlightBlocks(VisualsBlockHighlightType.MatchCombo);
		public void HighlightObjective() => HighlightBlocks(VisualsBlockHighlightType.Objective);
		public void HighlightDanger() => HighlightBlocks(VisualsBlockHighlightType.Danger);

		public void HighlightBlocks(VisualsBlockHighlightType type)
		{
			foreach(var block in m_Blocks) {
				block.SetHighlighted(type, true);
			}
		}

		public void RestoreToNormalBlocks()
		{
			foreach(var block in m_Blocks) {
				block.ClearAnyHighlights();
				block.ClearAllProperties();
				block.gameObject.SetActive(true);
			}

			foreach(var pair in m_FallTrailEffect_PreviewBlocks) {
				pair.Key.transform.localScale = pair.Value;
			}

			m_FallTrailEffect_PreviewBlocks.Clear();
			m_FallTrailEffectManager.ClearAllEffects();
		}

		public void FallTrailEffectPreview()
		{
			foreach(var block in m_Blocks) {
				block.gameObject.SetActive(Array.IndexOf(FallTrailEffectPreviewBlocks, block.gameObject) != -1);

				if (block.gameObject.activeSelf) {
					m_FallTrailEffect_PreviewBlocks.Add(new KeyValuePair<ConeVisualsBlock, Vector3>(block, block.transform.localScale));
				}
			}

			m_FallTrailEffect_PreviewBlocks.Sort((left, right) => left.Value.x.CompareTo(right.Value.x));

			m_FallTrailEffect_StartTime = Time.time;
			m_FallTrailEffectManager.StartFallTrailEffect(m_FallTrailEffect_PreviewBlocks.Select(p => p.Key.gameObject));
		}

		void Update()
		{
			if (m_FallTrailEffect_PreviewBlocks.Count == 0)
				return;

			if (m_FallTrailEffect_PreviewBlocks[m_FallTrailEffect_PreviewBlocks.Count - 1].Key.transform.localScale.x >= 1f)
				return;

			float fallDistance = FallSpeed * (Time.time - m_FallTrailEffect_StartTime);

			foreach (var pair in m_FallTrailEffect_PreviewBlocks) {

				pair.Key.transform.localScale = pair.Value + Vector3.one * fallDistance;
				if (pair.Key.transform.localScale.x > 1f) {
					pair.Key.transform.localScale = Vector3.one;
				}
			}
		}
#endif
	}
}