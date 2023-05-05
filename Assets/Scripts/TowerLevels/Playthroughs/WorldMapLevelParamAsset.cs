using System;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	[CreateAssetMenu(fileName = "Unknown_LevelParamAsset", menuName = "Tetris Tower/Level Param Asset")]
	public class WorldMapLevelParamAsset : SerializableAsset
	{
		public string LevelID;
		public GameObject WorldLandmarkPrefab;
		public Vector3 WorldMapPosition;

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

	public class LevelParamAssetConverter : SerializableAssetConverter<WorldMapLevelParamAsset>
	{
		public LevelParamAssetConverter(AssetsRepository repository) : base(repository) { }
	}
}