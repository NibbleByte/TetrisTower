using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// World play through with the world map.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_WorldLevelsSet", menuName = "Tetris Tower/World Levels Set")]
	public class WorldLevelsSet : SerializableAsset
	{
		public WorldMapLevelParamAsset[] Levels;

		public WorldMapLevelParamAsset GetLevel(string levelID)
		{
			foreach(var level in Levels) {
				if (level.LevelID == levelID)
					return level;
			}

			return null;
		}

		public void Validate(AssetsRepository repo)
		{
			foreach(WorldMapLevelParamAsset level in Levels) {
				if (level == null) {
					Debug.LogError($"{name} has missing level.", this);
					continue;
				}

				level.Validate(repo);
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Add level as sub asset")]
		private void AddLevelSubAsset()
		{
			var levelAsset = ScriptableObject.CreateInstance<WorldMapLevelParamAsset>();

			levelAsset.LevelID = levelAsset.name = $"Level-{Levels.Length:00}";

			Levels = Levels.Concat(new [] { levelAsset }).ToArray();

			UnityEditor.AssetDatabase.AddObjectToAsset(levelAsset, this);
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();
		}

		[ContextMenu("Refresh sub asset names")]
		private void RefreshSubAssetNames()
		{
			string ownedPath = UnityEditor.AssetDatabase.GetAssetPath(this);

			foreach(WorldMapLevelParamAsset level in Levels) {
				if (level && UnityEditor.AssetDatabase.GetAssetPath(level) == ownedPath && level.LevelID != level.name) {
					level.name = level.LevelID;
					UnityEditor.EditorUtility.SetDirty(level);
				}
			}

			UnityEditor.AssetDatabase.SaveAssets();
		}
#endif
	}

	public class WorldLevelsSetConverter : SerializableAssetConverter<WorldLevelsSet>
	{
		public WorldLevelsSetConverter(AssetsRepository repository) : base(repository) { }
	}
}