using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Visuals.Environment
{
	public class OverrideBlockTextureOverTime : MonoBehaviour, ILevelLoadedListener
	{
		public Texture OverrideTexture;
		public float OverrideSpeed = 0.01f;
		[Range(0f, 1f)]
		public float OverrideMax = 1f;

		public Vector2 PatternRandomOffset = new Vector2(20f, 20f);
		public float PatternScale = 50f;

		private GridLevelController m_LevelController;
		private ConeVisualsGrid m_VisualsGrid;

		private List<ConeVisualsBlock> m_Blocks = new ();

		private int m_OverrideTextureID;
		private int m_OverrideValueID;
		private int m_OverridePatternOffsetID;
		private int m_OverridePatternScaleID;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_LevelController);
			context.SetByType(out m_VisualsGrid);

			m_OverrideTextureID = Shader.PropertyToID("_Override_Map");
			m_OverrideValueID = Shader.PropertyToID("_Override_Value");
			m_OverridePatternOffsetID = Shader.PropertyToID("_Override_Pattern_Offset");
			m_OverridePatternScaleID = Shader.PropertyToID("_Override_Pattern_Scale");

			m_VisualsGrid.CreatedVisualsBlock += OnCreatedVisualsBlock;
			m_VisualsGrid.DestroyingVisualsBlock += OnDestroyingVisualsBlock;

			foreach(ConeVisualsBlock block in m_VisualsGrid.GetAllBlocks()) {
				OnCreatedVisualsBlock(block);
			}
		}

		public void OnLevelUnloading()
		{
			m_VisualsGrid.CreatedVisualsBlock -= OnCreatedVisualsBlock;
			m_VisualsGrid.DestroyingVisualsBlock -= OnDestroyingVisualsBlock;
			m_VisualsGrid = null;

			m_LevelController = null;

			m_Blocks.Clear();
		}

		private void OnCreatedVisualsBlock(ConeVisualsBlock block)
		{
			m_Blocks.Add(block);

			block.SetTexture(m_OverrideTextureID, OverrideTexture)
				.SetFloat(m_OverridePatternScaleID, PatternScale)
				.SetVector(m_OverridePatternOffsetID, UnityEngine.Random.insideUnitCircle * PatternRandomOffset)
				.ApplyProperties()
				;
		}

		private void OnDestroyingVisualsBlock(ConeVisualsBlock block)
		{
			m_Blocks.Remove(block);
		}

		void OnValidate()
		{
			if (OverrideTexture == null) {
				Debug.LogError($"No override texture specified for {name}.", this);
			}
		}

		void Update()
		{
			if (m_VisualsGrid == null)
				return;

			foreach(ConeVisualsBlock block in m_Blocks) {
				float value = block.GetFloat(m_OverrideValueID);

				value = Mathf.Clamp(value + OverrideSpeed * m_LevelController.DeltaPlayTime, 0f, OverrideMax);

				block.SetFloat(m_OverrideValueID, value)
					.ApplyProperties();
			}
		}

		[ContextMenu("Set Blocks To Max Override")]
		private void SetBlocksToMaxOverride()
		{
			foreach(ConeVisualsBlock block in m_Blocks) {
				block.SetFloat(m_OverrideValueID, OverrideMax)
					.ApplyProperties();
			}
		}
	}

}