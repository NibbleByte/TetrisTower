using System;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;


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

		[JsonIgnore]
		public Sprite PreviewImage; // TODO: This doesn't support modding easily. Make an option to load by image name from streaming assets.

		// TODO: Not used anymore.
		[NonSerialized]
		public int StarsCostNOTUSED = -1;  // Cost to unlock this level.

		public int[] ScoreToStars;  // Score needed to earn the corresponding star.

		public int CalculateStarsEarned(int score)
		{
			return Array.FindLastIndex(ScoreToStars, s => score >= s) + 1;
		}

		public override void Validate(AssetsRepository repo, UnityEngine.Object context)
		{
			if (string.IsNullOrEmpty(LevelID)) {
				Debug.LogError($"{context} doesn't have LevelID set!", context);
			}

			// 0 is considered a starting level.
			//if (StarsCostNOTUSED < 0) {
			//	Debug.LogError($"{context} needs to have some StarsCost assigned.", context);
			//}

			if (ScoreToStars == null || ScoreToStars.Length != 3) {
				Debug.LogError($"{context} needs to have 3 ScoreToStars.", context);
			} else {
				for(int i = 1; i < ScoreToStars.Length; i++) {
					if (ScoreToStars[i - 1] >= ScoreToStars[i]) {
						Debug.LogError($"{context} ScoreToStars isn't sorted!", context);
						break;
					}
				}
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
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			//position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			WorldLevelsSet worldLevelsSet = (WorldLevelsSet)property.serializedObject.targetObject;
			string[] levelIDs = worldLevelsSet.Levels.Select(x => x.LevelID).ToArray();

			links.Clear();
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsLink.LevelID1)));
			links.Add(property.FindPropertyRelative(nameof(WorldLevelsLink.LevelID2)));

			float linkHeight = EditorGUI.GetPropertyHeight(links[0]);

			const float padding = 4f;

			for (int i = 0; i < links.Count; ++i) {
				SerializedProperty linkProp = links[i];

				Rect idPos = position;
				idPos.width = position.width / links.Count - links.Count * padding;
				idPos.x = position.x + i * position.width / links.Count + i * padding;
				idPos.height = linkHeight;

				EditorGUI.BeginChangeCheck();
				int selectedIndex = EditorGUI.Popup(idPos, Array.IndexOf(levelIDs, linkProp.stringValue), levelIDs);
				if (EditorGUI.EndChangeCheck()) {
					linkProp.stringValue = levelIDs[selectedIndex];
				}
			}

			EditorGUI.EndProperty();
		}
	}
#endif
}