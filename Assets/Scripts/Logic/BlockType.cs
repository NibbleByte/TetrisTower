using System;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	[CreateAssetMenu(fileName = "Unknown_Block", menuName = "Tetris Tower/Block")]
	public class BlockType : SerializableAsset
	{
		public GameObject Prefab;
	}

	[Serializable]
	public class BlocksShape : GridShape<BlockType>
	{
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(BlocksShape))]
	public class BlocksShapeDrawer : Tools.SerializeReferenceCreatorDrawer<BlocksShape>
	{
	}
#endif

	public class BlockTypeConverter : SerializableAssetConverter<BlockType>
	{
		public BlockTypeConverter(AssetsRepository repository) : base(repository) { }
	}
}