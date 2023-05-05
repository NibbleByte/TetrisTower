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
		[Serializable]
		public struct LevelsLink
		{
			public string LevelID1;
			public string LevelID2;
		}

		public WorldMapLevelParamAsset StartLevel;
		public WorldMapLevelParamAsset[] Levels;

		public LevelsLink[] LevelsLinks;

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

				if (LevelsLinks.All(ll => ll.LevelID1 != level.LevelID && ll.LevelID2 != level.LevelID)) {
					Debug.LogError($"{name} level \"{level.LevelID}\" has no links to other levels.", this);
				}
			}

			foreach(LevelsLink link in LevelsLinks) {
				if (string.IsNullOrWhiteSpace(link.LevelID1) || string.IsNullOrWhiteSpace(link.LevelID2)) {
					Debug.LogError($"{name} has empty.", this);
					continue;
				}

				if (link.LevelID1 == link.LevelID2) {
					Debug.LogError($"{name} has a link that links one level to itself -> \"{link.LevelID1}\"", this);
					continue;
				}

				if (GetLevel(link.LevelID1) == null) {
					Debug.LogError($"{name} has invalid link to \"{link.LevelID1}\".", this);
				}

				if (GetLevel(link.LevelID2) == null) {
					Debug.LogError($"{name} has invalid link to \"{link.LevelID2}\".", this);
				}
			}
		}

#if UNITY_EDITOR
		[ContextMenu("Add level as sub asset")]
		private void AddLevelSubAsset()
		{
			var levelAsset = ScriptableObject.CreateInstance<WorldMapLevelParamAsset>();

			levelAsset.LevelID = levelAsset.name = $"Level-{Levels.Length:00}";

			Levels = Levels.Concat(new [] { levelAsset }).ToArray();

			AssetDatabase.AddObjectToAsset(levelAsset, this);
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}

		[ContextMenu("Refresh sub asset names")]
		private void RefreshSubAssetNames()
		{
			string ownedPath = AssetDatabase.GetAssetPath(this);

			foreach(WorldMapLevelParamAsset level in Levels) {
				if (level && AssetDatabase.GetAssetPath(level) == ownedPath && level.LevelID != level.name) {
					level.name = level.LevelID;
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

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(WorldLevelsSet.LevelsLink))]
	internal class LevelsLinkPropertyDrawer : PropertyDrawer
	{
		List<SerializedProperty> links = new List<SerializedProperty>(2);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(WorldLevelsSet.LevelsLink.LevelID1))) * 2 + 4f /* Padding */;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			//position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			links.Clear();
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsSet.LevelsLink.LevelID1)));
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsSet.LevelsLink.LevelID2)));

			float linkHeight = EditorGUI.GetPropertyHeight(links[0]);

			for(int i = 0; i < links.Count; ++i) {
				SerializedProperty linkProp = links[i];

				Rect idPos = position;
				idPos.width *= 0.75f;
				idPos.height = linkHeight;
				idPos.y = position.y + i * linkHeight;

				Rect assetPos = idPos;
				assetPos.x += idPos.width;
				assetPos.width = position.width - idPos.width;

				EditorGUI.PropertyField(idPos, linkProp);
				var levelObject = EditorGUI.ObjectField(assetPos, null, typeof(WorldMapLevelParamAsset), false) as WorldMapLevelParamAsset;
				if (levelObject) {
					linkProp.stringValue = levelObject.LevelID;
				}
			}


			EditorGUI.EndProperty();
		}
	}
#endif
}