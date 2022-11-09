using System.Linq;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[System.Serializable]
	public struct BlockSkin
	{
		public BlockType BlockType;
		public GameObject Prefab;
		public Sprite Icon;
	}

	[CreateAssetMenu(fileName = "UnknownBlocksSet", menuName = "Tetris Tower/Blocks Set")]
	public class BlocksSkinSet : SerializableAsset
	{
		[Tooltip("Blocks to be used along the playthroughs. Can be overridden by the playthrough template.")]
		public BlockSkin[] BlockSkins;

		private BlockSkin[] m_NormalBlocks;

		public BlockSkin GetSkinFor(BlockType blockType)
		{
			foreach(BlockSkin skin in BlockSkins) {
				if (skin.BlockType == blockType)
					return skin;
			}

			return default;
		}

		public GameObject GetPrefabFor(BlockType blockType) => GetSkinFor(blockType).Prefab;
		public Sprite GetIconFor(BlockType blockType) => GetSkinFor(blockType).Icon;

		public BlockSkin[] GetNormalBlocks()
		{
			if (m_NormalBlocks == null) {
				m_NormalBlocks = BlockSkins.Where(s => s.BlockType.IsNormalBlock()).OrderBy(s => s.BlockType).ToArray();
			}

			return m_NormalBlocks;
		}

		public void Validate(AssetsRepository repo, Object context)
		{
			// HACK: for disabled assembly reloads.
			m_NormalBlocks = null;

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