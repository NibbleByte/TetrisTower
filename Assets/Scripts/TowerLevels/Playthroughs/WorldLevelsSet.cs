using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// World play through with the world map.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_WorldLevelsSet", menuName = "Tetris Tower/World Levels Set")]
	public class WorldLevelsSet : SerializableAsset
	{
		[Serializable]
		public struct WorldLevelDescription
		{
			public string LevelID;
			public Vector2 Position;
			public LevelParamAsset LevelAsset;

			public bool IsValid => !string.IsNullOrEmpty(LevelID);
		}

		public WorldLevelDescription[] Levels;

		public WorldLevelDescription GetLevel(string levelID)
		{
			foreach(var level in Levels) {
				if (level.LevelID == levelID)
					return level;
			}

			return default;
		}

		public void Validate(AssetsRepository repo)
		{
			foreach(WorldLevelDescription level in Levels) {
				level.LevelAsset.Validate(repo);
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Add level as sub asset")]
		private void AddLevelSubAsset()
		{
			var levelAsset = ScriptableObject.CreateInstance<LevelParamAsset>();

			levelAsset.name = $"Level-{Levels.Length:00}";

			WorldLevelDescription worldLevel = new WorldLevelDescription() {
				LevelID = levelAsset.name,
				LevelAsset = levelAsset,
			};

			Levels = Levels.Concat(new WorldLevelDescription[] { worldLevel }).ToArray();

			UnityEditor.AssetDatabase.AddObjectToAsset(levelAsset, this);
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();
		}

		[ContextMenu("Refresh sub asset names")]
		private void RefreshSubAssetNames()
		{
			string ownedPath = UnityEditor.AssetDatabase.GetAssetPath(this);

			foreach(WorldLevelDescription level in Levels) {
				if (level.LevelAsset && UnityEditor.AssetDatabase.GetAssetPath(level.LevelAsset) == ownedPath && level.LevelID != level.LevelAsset.name) {
					level.LevelAsset.name = level.LevelID;
					UnityEditor.EditorUtility.SetDirty(level.LevelAsset);
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