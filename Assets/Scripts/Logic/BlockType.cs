using DevLocker.GFrame.Utils;
using System;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum MatchType
	{
		None,
		MatchSame,
		MatchAny,
	}

	[CreateAssetMenu(fileName = "Unknown_Block", menuName = "Tetris Tower/Block")]
	public class BlockType : SerializableAsset
	{
		public MatchType MatchType = MatchType.MatchSame;
		public Sprite Icon;
		public GameObject Prefab3D;
		public GameObject Prefab2D;
	}

	[Serializable]
	public class BlocksShape : GridShape<BlockType>
	{
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(BlocksShape))]
	public class BlocksShapeDrawer : SerializeReferenceCreatorDrawer<BlocksShape>
	{
	}
#endif

	public class BlockTypeConverter : SerializableAssetConverter<BlockType>
	{
		public BlockTypeConverter(AssetsRepository repository) : base(repository) { }
	}
}