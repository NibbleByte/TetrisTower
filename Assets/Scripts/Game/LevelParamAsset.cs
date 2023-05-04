using System;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "Unknown_LevelParamAsset", menuName = "Tetris Tower/Level Param Asset")]
	public class LevelParamAsset : SerializableAsset
	{
		public LevelParamData LevelParam;

		public void Validate(AssetsRepository repo)
		{
			LevelParam.Validate(repo, this);
		}

#if UNITY_EDITOR
		[ContextMenu("Delete sub asset")]
		private void DeleteLevelSubAsset()
		{
			UnityEditor.AssetDatabase.RemoveObjectFromAsset(this);
			DestroyImmediate(this, true);
			UnityEditor.AssetDatabase.SaveAssets();
		}
#endif
	}

	public class LevelParamAssetConverter : SerializableAssetConverter<LevelParamAsset>
	{
		public LevelParamAssetConverter(AssetsRepository repository) : base(repository) { }
	}
}