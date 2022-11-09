using DevLocker.GFrame.Utils;
using System;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Logic
{
	public enum BlockType
	{
		None = 0,

		B1 = 1,
		B2 = 2,
		B3 = 3,
		B4 = 4,
		B5 = 5,
		B6 = 6,
		B7 = 7,
		B8 = 8,
		B9 = 9,
		B10 = 10,
		B11 = 11,
		B12 = 12,
		B13 = 13,
		B14 = 14,
		B15 = 15,
		B16 = 16,

		SpecialWildBlock = 100,

		SpecialBlockSmite = 200,

		SpecialRowSmite = 300,


		StaticBlock = 1000,
		WonBonusBlock = 1010,
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

}