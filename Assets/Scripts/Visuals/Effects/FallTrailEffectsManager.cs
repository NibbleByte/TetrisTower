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
			public bool ParentSameAsOriginal;
			public float StartBottomScale = 0;
			public float LastBottomScale = 0;
			public Quaternion LastBottomRotation = Quaternion.identity;

			public ParticleSystem ParticlesInstance;

			public void Destroy()
			{
				if (ParticlesInstance) {
					GameObject.Destroy(ParticlesInstance.gameObject, ParticlesInstance.main.duration);
				}

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

		[Tooltip("Optional - Partical effect to be moved with the bottom falling block of the group.")]
		public ParticleSystem ParticlesTemplate;

		private int m_TopScalePropID;
		private int m_BottomPropID;
		private int m_AlphaPropID;

		private List<BlocksGroup> m_BlockGroups = new List<BlocksGroup>();


		void Awake()
		{
			m_TopScalePropID = Shader.PropertyToID("_TopScale");
			m_BottomPropID = Shader.PropertyToID("_BottomScale");
			m_AlphaPropID = Shader.PropertyToID("_Alpha");

			if (ParticlesTemplate) {
				ParticlesTemplate.gameObject.SetActive(false);
			}
		}

		public void StartFallTrailEffect(IEnumerable<GameObject> blocks, Transform parent = null)
		{
			var group = new BlocksGroup();

			if (parent == null) {
				parent = transform;
			}

			foreach(GameObject block in blocks) {

				GameObject effectGO = Instantiate(block, block.transform.position, block.transform.rotation, parent);
				ConeVisualsBlock effectBlock = effectGO.GetComponent<ConeVisualsBlock>();

				if (effectBlock == null) {
					effectBlock = effectGO.AddComponent<ConeVisualsBlock>();
				}

				effectBlock.RestoreToNormal();	// Just in case, if it came highlighted or something.
				effectBlock.SetFallTrailEffect();
				effectBlock.gameObject.name = $"_{block.name}_Effect";
				effectBlock.transform.localScale = block.transform.localScale + new Vector3(AddSideScale, 0f, 0f);
				effectBlock.transform.position = block.transform.position + block.transform.forward * AddForwardOffset;

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

			EffectBlockEntry groupBottomEntry = group[group.Count - 1];

			group.StartBottomScale = groupBottomEntry.OriginalBlockGO.transform.localScale.y;
			group.ParentSameAsOriginal = group[0].OriginalBlockGO.transform.parent == parent;

			if (ParticlesTemplate && groupBottomEntry.OriginalBlockGO.GetComponent<Collider>()) {
				group.ParticlesInstance = Instantiate(ParticlesTemplate);
				group.ParticlesInstance.transform.position = groupBottomEntry.OriginalBlockGO.GetComponent<Collider>().bounds.center;
				group.ParticlesInstance.gameObject.SetActive(true);
			}

			m_BlockGroups.Add(group);
		}

		public void ClearAllEffects()
		{
			foreach(BlocksGroup group in m_BlockGroups) {

				if (group.ParticlesInstance) {
					GameObject.Destroy(group.ParticlesInstance.gameObject, group.ParticlesInstance.main.duration);
				}

				foreach(EffectBlockEntry entry in group) {
					GameObject.Destroy(entry.EffectBlock.gameObject);
				}
			}

			m_BlockGroups.Clear();
		}

		private void Update()
		{
			foreach(BlocksGroup group in m_BlockGroups) {
				EffectBlockEntry groupBottomEntry = group[group.Count - 1];

				// Keep this in Update() as the Crest Ocean Renderer + Underwater suppresses particle emitting when moving on LateUpdate().
				// This of course means that particles will lag behind a bit.
				if (group.ParticlesInstance && groupBottomEntry.OriginalBlockGO) {
					group.ParticlesInstance.transform.position = groupBottomEntry.OriginalBlockGO.GetComponent<Collider>().bounds.center;
				}
			}
		}

		// LateUpdate() so it gets executed after the GridLevelController that shoots Won animation. Hacky hacky. Sergey was right.
		void LateUpdate()
		{
			for(int groupIndex = 0; groupIndex < m_BlockGroups.Count; ++groupIndex) {
				BlocksGroup group = m_BlockGroups[groupIndex];

				EffectBlockEntry groupBottomEntry = group[group.Count - 1];

				float groupBottomScale = group.LastBottomScale;
				Quaternion groupBottomRotation = group.LastBottomRotation;
				if (groupBottomEntry.OriginalBlockGO) {
					groupBottomScale = groupBottomEntry.OriginalBlockGO.transform.localScale.y;
					groupBottomRotation = groupBottomEntry.OriginalBlockGO.transform.rotation;
				}

				bool hasChanges = group.LastBottomScale != groupBottomScale || group.LastBottomRotation != groupBottomRotation;
				group.LastBottomScale = groupBottomScale;
				group.LastBottomRotation = groupBottomRotation;

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

						if (group.ParentSameAsOriginal && entry.OriginalBlockGO.transform.parent != entry.EffectBlock.transform.parent) {
							entry.EffectBlock.transform.SetParent(entry.OriginalBlockGO.transform.parent);
						}
					} else {
						// If original parent was rotating and we lost the object, we most likely don't want to continue rotating (fixes won animation).
						if (group.ParentSameAsOriginal && entry.EffectBlock.transform.parent) {
							entry.EffectBlock.transform.SetParent(null);
						}
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