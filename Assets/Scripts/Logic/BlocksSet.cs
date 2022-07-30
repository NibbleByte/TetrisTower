using System.Collections.Generic;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CreateAssetMenu(fileName = "UnknownBlocksSet", menuName = "Tetris Tower/Blocks Set")]
	public class BlocksSet : SerializableAsset
	{
		[Tooltip("Blocks to be used along the playthroughs. The index decides the order of appearance. Can be overridden by the playthrough template.")]
		public BlockType[] Blocks;

		[Tooltip("Block to be used as \"Wild\" or \"Joker\" that matches with any other type of block.")]
		public BlockType WildBlock;

		[Tooltip("Block to be used for bonus blocks in the won animation at the end.")]
		public BlockType WonBonusBlock;

		public IEnumerable<BlockType> AllBlocks
		{
			get {
				foreach(BlockType block in Blocks) {
					if (block)
						yield return block;
				}

				if (WildBlock) yield return WildBlock;
				if (WonBonusBlock) yield return WonBonusBlock;
			}
		}

		public void Validate(AssetsRepository repo, Object context)
		{
			if (!repo.IsRegistered(this)) {
				Debug.LogError($"BlocksSet {name} is not registered for serialization, at {context}.{name}", context);
			}

			foreach(BlockType serializedBlock in AllBlocks) {
				if (!repo.IsRegistered(serializedBlock)) {
					Debug.LogError($"Block {serializedBlock.name} is not registered for serialization in {nameof(BlocksSet)} at {context}.{name}", context);
				}
			}

			for (int i = 0; i < Blocks.Length; i++) {
				var block = Blocks[i];

				if (block == null) {
					Debug.LogError($"Missing {i}-th block from {nameof(Blocks)} in {nameof(BlocksSet)} at {context}.{name}", context);
					continue;
				}

				if (block.MatchType != MatchType.MatchSame) {
					Debug.LogError($"Block {block.name} should be matchable, in {nameof(BlocksSet)} at {context}.{name}", context);
				}
			}

			if (WildBlock && WildBlock.MatchType != MatchType.MatchAny) {
				Debug.LogError($"{nameof(WildBlock)} {WildBlock.name} should match anything, in {nameof(BlocksSet)} at {context}.{name}", context);
			}

			if (WonBonusBlock == null) {
				Debug.LogError($"{nameof(WonBonusBlock)} missing in {nameof(BlocksSet)} at {context}.{name}", context);
			} else if (WonBonusBlock.MatchType != MatchType.None) {
				Debug.LogError($"{nameof(WonBonusBlock)} {WonBonusBlock.name} shouldn't be matchable, in {nameof(BlocksSet)} at {context}.{name}", context);
			}
		}
	}

	public class BlocksSetConverter : SerializableAssetConverter<BlocksSet>
	{
		public BlocksSetConverter(AssetsRepository repository) : base(repository) { }
	}
}