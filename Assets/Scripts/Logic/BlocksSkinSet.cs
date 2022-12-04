using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[Serializable]
	public struct BlockSkin
	{
		public BlockType BlockType;
		public GameObject Prefab;
		public Sprite Icon;

		public bool IsValid => BlockType != BlockType.None;
	}

	/// <summary>
	/// Combines multiple skin sets, latest overriding previous ones (like a stack).
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class BlocksSkinStack
	{
		public bool IsEmpty => m_BlocksSets.Count == 0;

		[SerializeField]
		private List<BlocksSkinSet> m_BlocksSets = new List<BlocksSkinSet>();

		[JsonIgnore]
		private Dictionary<BlockType, BlockSkin> m_MergedSkins = null;

		[JsonIgnore]
		private BlockSkin[] m_NormalBlocks;

		// For serailization
		public BlocksSkinStack()
		{
		}

		public BlocksSkinStack(params BlocksSkinSet[] skinSets)
		{
			AppendSkinSets(skinSets);
		}

		public void AppendSkinSets(params BlocksSkinSet[] skinSets)
		{
			foreach(BlocksSkinSet skinSet in skinSets) {
				if (skinSet) {
					m_BlocksSets.Add(skinSet);
				}
			}

			m_MergedSkins = null;
			m_NormalBlocks = null;
		}

		public BlockSkin GetSkinFor(BlockType blockType)
		{
			// Lazy recreate data as it may be coming from save.
			if (m_MergedSkins == null) {
				m_MergedSkins = new Dictionary<BlockType, BlockSkin>();
				var normalSkins = new SortedDictionary<BlockType, BlockSkin>();

				// Later sets will override previous ones.
				foreach (BlocksSkinSet skinSet in m_BlocksSets) {
					foreach(BlockSkin skin in skinSet.BlockSkins) {
						m_MergedSkins[skin.BlockType] = skin;

						if (skin.BlockType.IsNormalBlock()) {
							normalSkins[skin.BlockType] = skin;
						}
					}
				}

				m_NormalBlocks = normalSkins.Values.ToArray();
			}

			return m_MergedSkins.TryGetValue(blockType, out BlockSkin result) ? result : default;
		}

		public BlockSkin[] GetNormalBlocks()
		{
			if (m_NormalBlocks == null) {
				GetSkinFor(BlockType.B1);
			}

			return m_NormalBlocks;
		}

		public GameObject GetPrefabFor(BlockType blockType) => GetSkinFor(blockType).Prefab;
		public Sprite GetIconFor(BlockType blockType) => GetSkinFor(blockType).Icon;

		public void Validate(AssetsRepository repo, UnityEngine.Object context)
		{
			foreach(BlocksSkinSet skinSet in m_BlocksSets) {
				skinSet.Validate(repo, context);
			}
		}
	}

	[CreateAssetMenu(fileName = "UnknownBlocksSet", menuName = "Tetris Tower/Blocks Set")]
	public class BlocksSkinSet : SerializableAsset
	{
		[Tooltip("Blocks to be used along the playthroughs. Can be overridden by the playthrough template.")]
		public BlockSkin[] BlockSkins;

		public void Validate(AssetsRepository repo, UnityEngine.Object context)
		{
			if (!repo.IsRegistered(this)) {
				Debug.LogError($"{nameof(BlocksSkinSet)} {name} is not registered for serialization, at {context}.{name}", context);
			}

			for (int i = 0; i < BlockSkins.Length; i++) {
				BlockSkin skin = BlockSkins[i];

				if (skin.BlockType == BlockType.None) {
					Debug.LogError($"Block type not set for the {i}-th skin from {nameof(BlockSkins)} in {nameof(BlocksSkinSet)} at {context}.{name}", context);
					continue;
				}

				if (skin.Prefab == null) {
					Debug.LogError($"Missing prefab in the {i}-th skin for {skin.BlockType} from {nameof(BlockSkins)} in {nameof(BlocksSkinSet)} at {context}.{name}", context);
					continue;
				}
			}
		}
	}

	public class BlocksSkinSetConverter : SerializableAssetConverter<BlocksSkinSet>
	{
		public BlocksSkinSetConverter(AssetsRepository repository) : base(repository) { }
	}
}