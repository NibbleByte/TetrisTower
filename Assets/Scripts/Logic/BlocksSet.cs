using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CreateAssetMenu(fileName = "UnknownBlocksSet", menuName = "Tetris Tower/Blocks Set")]
	public class BlocksSet : SerializableAsset
	{
		[Tooltip("Blocks to be used along the playthroughs. The index decides the order of appearance. Can be overridden by the playthrough template.")]
		public BlockType[] Blocks;

		[Tooltip("Block to be used for bonus blocks in the won animation at the end.")]
		public BlockType WonBonusBlock;

		public void Validate(AssetsRepository repo, Object context)
		{
			if (!repo.IsRegistered(this)) {
				Debug.LogError($"BlocksSet {name} is not registered for serialization, at {context}.{name}", context);
			}

			for (int i = 0; i < Blocks.Length; i++) {
				var block = Blocks[i];

				if (block == null) {
					Debug.LogError($"Missing {i}-th block from {nameof(Blocks)} in {nameof(BlocksSet)} at {context}.{name}", context);
					continue;
				}

				if (!repo.IsRegistered(block)) {
					Debug.LogError($"Block {block.name} is not registered for serialization, from {nameof(Blocks)} in {nameof(BlocksSet)} at {context}.{name}", context);
				}
			}
		}
	}

	public class BlocksSetConverter : SerializableAssetConverter<BlocksSet>
	{
		public BlocksSetConverter(AssetsRepository repository) : base(repository) { }
	}
}