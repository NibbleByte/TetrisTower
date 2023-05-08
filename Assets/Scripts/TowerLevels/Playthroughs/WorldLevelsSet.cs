using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// World play through with the world map.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_WorldLevelsSet", menuName = "Tetris Tower/World Levels Set")]
	public class WorldLevelsSet : SerializableAsset
	{
		public WorldMapLevelParamAsset StartLevel;

		[SerializeField]
		private WorldMapLevelParamAsset[] m_Levels;

		public int LevelsCount => m_Levels.Length;
		public IEnumerable<WorldMapLevelParamData> Levels => m_Levels.Where(asset => asset != null).Select(asset => asset.LevelParam);

		public WorldLevelsLink[] LevelsLinks;

		public WorldMapLevelParamData GetLevelData(string levelID)
		{
			foreach(var level in m_Levels) {
				if (level.LevelParam.LevelID == levelID)
					return level.LevelParam;
			}

			return null;
		}

		public IEnumerable<string> GetLinkedLevelIDsFor(string levelID)
		{
			foreach(WorldLevelsLink link in LevelsLinks) {
				if (link.LevelID1 == levelID)
					yield return link.LevelID2;

				if (link.LevelID2 == levelID)
					yield return link.LevelID1;
			}
		}

		public void Validate(AssetsRepository repo)
		{
			foreach(WorldMapLevelParamAsset level in m_Levels) {
				if (level == null) {
					Debug.LogError($"{name} has missing level.", this);
					continue;
				}

				level.Validate(repo);

				if (LevelsLinks.All(ll => ll.LevelID1 != level.LevelParam.LevelID && ll.LevelID2 != level.LevelParam.LevelID)) {
					Debug.LogError($"{name} level \"{level.LevelParam.LevelID}\" has no links to other levels.", this);
				}
			}

			foreach(WorldLevelsLink link in LevelsLinks) {
				if (string.IsNullOrWhiteSpace(link.LevelID1) || string.IsNullOrWhiteSpace(link.LevelID2)) {
					Debug.LogError($"{name} has empty.", this);
					continue;
				}

				if (link.LevelID1 == link.LevelID2) {
					Debug.LogError($"{name} has a link that links one level to itself -> \"{link.LevelID1}\"", this);
					continue;
				}

				if (GetLevelData(link.LevelID1) == null) {
					Debug.LogError($"{name} has invalid link to \"{link.LevelID1}\".", this);
				}

				if (GetLevelData(link.LevelID2) == null) {
					Debug.LogError($"{name} has invalid link to \"{link.LevelID2}\".", this);
				}
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Add level as sub asset")]
		private void AddLevelSubAsset()
		{
			var levelAsset = ScriptableObject.CreateInstance<WorldMapLevelParamAsset>();

			levelAsset.LevelParam.LevelID = levelAsset.name = $"Level-{m_Levels.Length:00}";

			m_Levels = m_Levels.Concat(new [] { levelAsset }).ToArray();

			AssetDatabase.AddObjectToAsset(levelAsset, this);
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		[ContextMenu("Refresh sub asset names")]
		private void RefreshSubAssetNames()
		{
			string ownedPath = AssetDatabase.GetAssetPath(this);

			foreach(WorldMapLevelParamAsset level in m_Levels) {
				if (level && AssetDatabase.GetAssetPath(level) == ownedPath && level.LevelParam.LevelID != level.name) {
					level.name = level.LevelParam.LevelID;
					EditorUtility.SetDirty(level);
				}
			}

			AssetDatabase.SaveAssets();
		}
#endif
	}

	public class WorldLevelsSetConverter : SerializableAssetConverter<WorldLevelsSet>
	{
		public WorldLevelsSetConverter(AssetsRepository repository) : base(repository) { }
	}
}