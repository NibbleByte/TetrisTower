using System;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TetrisTower.TowerLevels.Playthroughs
{
	[Serializable]
	public class WorldMapLevelParamData : LevelParamData
	{
		[Header("World Map Info")]
		public string LevelID;
		public Vector2 WorldMapPosition;
		public Sprite Thumbnail; // TODO: This doesn't support modding easily. Make an option to load by image name from streaming assets.

		public override void Validate(AssetsRepository repo, UnityEngine.Object context)
		{
			if (string.IsNullOrEmpty(LevelID)) {
				Debug.LogError($"{context} doesn't have LevelID set!", context);
			}

			base.Validate(repo, context);
		}
	}

	[Serializable]
	public struct WorldLevelsLink
	{
		public string LevelID1;
		public string LevelID2;
	}

	[CreateAssetMenu(fileName = "Unknown_LevelParamAsset", menuName = "Tetris Tower/Level Param Asset")]
	public class WorldMapLevelParamAsset : SerializableAsset
	{
		public WorldMapLevelParamData LevelParam;

		public void Validate(AssetsRepository repo)
		{
			LevelParam.Validate(repo, this);
		}

#if UNITY_EDITOR
		[ContextMenu("Delete sub asset")]
		private void DeleteLevelSubAsset()
		{
			AssetDatabase.RemoveObjectFromAsset(this);
			DestroyImmediate(this, true);
			AssetDatabase.SaveAssets();
		}
#endif
	}

	public class LevelParamAssetConverter : SerializableAssetConverter<WorldMapLevelParamAsset>
	{
		public LevelParamAssetConverter(AssetsRepository repository) : base(repository) { }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(WorldLevelsLink))]
	internal class LevelsLinkPropertyDrawer : PropertyDrawer
	{
		List<SerializedProperty> links = new List<SerializedProperty>(2);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(WorldLevelsLink.LevelID1))) * 2 + 4f /* Padding */;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			//position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			links.Clear();
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsLink.LevelID1)));
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsLink.LevelID2)));

			float linkHeight = EditorGUI.GetPropertyHeight(links[0]);

			for (int i = 0; i < links.Count; ++i) {
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
					linkProp.stringValue = levelObject.LevelParam.LevelID;
				}
			}


			EditorGUI.EndProperty();
		}
	}
#endif
}