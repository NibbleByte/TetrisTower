using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class FallTrailEffectsManager : MonoBehaviour
	{
		private struct EffectBlockEntry
		{
			public GameObject OriginalBlockGO;
			public Vector3 LastOriginalScale;
			public ConeVisualsBlock EffectBlock;
			public MaterialPropertyBlock MaterialProperties;
		}

		private class BlocksGroup : List<EffectBlockEntry>
		{
			public float StartBottomScale = 0;
			public float LastBottomScale = 0;

			public void Destroy()
			{
				foreach(EffectBlockEntry entry in this) {
					GameObject.Destroy(entry.EffectBlock.gameObject);
				}

				Clear();
			}
		}

		public float AddSideScale = 0.025f;
		public float AddForwardOffset = 0.1f;
		public float DampSpeed = 0.04f;
		public float FinishDistance = 0.006f;
		public AnimationCurve AlphaCurve = AnimationCurve.EaseInOut(0f, 0f, 0.1f, 0.6f);

		private int m_TopScalePropID;
		private int m_BottomPropID;
		private int m_AlphaPropID;

		private List<BlocksGroup> m_BlockGroups = new List<BlocksGroup>();


		void Awake()
		{
			m_TopScalePropID = Shader.PropertyToID("_TopScale");
			m_BottomPropID = Shader.PropertyToID("_BottomScale");
			m_AlphaPropID = Shader.PropertyToID("_Alpha");
		}

		public void StartFallTrailEffect(IEnumerable<GameObject> blocks)
		{
			var group = new BlocksGroup();

			foreach(GameObject block in blocks) {

				GameObject effectGO = Instantiate(block, block.transform.position, block.transform.rotation, block.transform.parent);
				ConeVisualsBlock effectBlock = effectGO.GetComponent<ConeVisualsBlock>();

				if (effectBlock == null) {
					effectBlock = effectGO.AddComponent<ConeVisualsBlock>();
				}

				effectBlock.RestoreToNormal();	// Just in case, if it came highlighted or something.
				effectBlock.SetFallTrailEffect();
				effectBlock.gameObject.name = $"_{block.name}_Effect";
				effectBlock.transform.localScale = block.transform.localScale + new Vector3(AddSideScale, 0f, 0f);
				effectBlock.transform.localPosition = block.transform.localPosition + new Vector3(0f, 0f, AddForwardOffset);

				var entry = new EffectBlockEntry() {
					OriginalBlockGO = block,
					LastOriginalScale = block.transform.localScale,
					EffectBlock = effectBlock,
					MaterialProperties = new MaterialPropertyBlock(),
				};

				// Make sure alpha is off for the first frame or it flashes.
				entry.MaterialProperties.SetFloat(m_AlphaPropID, 0f);
				entry.EffectBlock.SetMaterialPropertyBlock(entry.MaterialProperties);

				group.Add(entry);
			}

			if (group.Count == 0) {
				Debug.LogError($"Fall trail effect requested with no blocks! Abort!", this);
				return;
			}


			group.Sort((left, right) => left.OriginalBlockGO.transform.localScale.y.CompareTo(right.OriginalBlockGO.transform.localScale.y));

			group.StartBottomScale = group[group.Count - 1].OriginalBlockGO.transform.localScale.y;

			m_BlockGroups.Add(group);
		}

		public void ClearAllEffects()
		{
			foreach(BlocksGroup group in m_BlockGroups) {
				foreach(EffectBlockEntry entry in group) {
					GameObject.Destroy(entry.EffectBlock.gameObject);
				}
			}

			m_BlockGroups.Clear();
		}

		void Update()
		{
			for(int groupIndex = 0; groupIndex < m_BlockGroups.Count; ++groupIndex) {
				BlocksGroup group = m_BlockGroups[groupIndex];

				EffectBlockEntry groupBottomEntry = group[group.Count - 1];

				float groupBottomScale = group.LastBottomScale;
				if (groupBottomEntry.OriginalBlockGO) {
					groupBottomScale = groupBottomEntry.OriginalBlockGO.transform.localScale.y;
				}

				bool hasChanges = group.LastBottomScale != groupBottomScale;
				group.LastBottomScale = groupBottomScale;

				group.StartBottomScale = Mathf.Lerp(group.StartBottomScale, groupBottomScale, DampSpeed);
				float alpha = AlphaCurve.Evaluate(groupBottomScale - group.StartBottomScale);

				if (hasChanges == false && (groupBottomScale - group.StartBottomScale) < FinishDistance) {
					group.Destroy();
					m_BlockGroups.RemoveAt(groupIndex);
					groupIndex--;
					continue;
				}

				float scalePerPoint = (groupBottomScale - group.StartBottomScale) / group.Count;

				for(int entryIndex = group.Count - 1; entryIndex >= 0 ; --entryIndex) {
					EffectBlockEntry entry = group[entryIndex];

					if (entry.OriginalBlockGO) {
						entry.EffectBlock.transform.localScale = entry.OriginalBlockGO.transform.localScale + new Vector3(AddSideScale, 0f, 0f);
					}

					float topScale = 1f - (group.Count - 1 - (entryIndex - 1)) * scalePerPoint;
					float bottomScale = 1f - (group.Count - 1 - entryIndex) * scalePerPoint;

					entry.MaterialProperties.SetFloat(m_BottomPropID, bottomScale);
					entry.MaterialProperties.SetFloat(m_TopScalePropID, topScale);

					entry.MaterialProperties.SetFloat(m_AlphaPropID, alpha);

					entry.EffectBlock.SetMaterialPropertyBlock(entry.MaterialProperties);
				}
			}
		}
	}
}