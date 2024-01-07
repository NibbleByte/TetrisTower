using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.Input.UIInputDisplay;
using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;
using System.Linq;
using DevLocker.Utils;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownGameSettings", menuName = "Tetris Tower/Game Config")]
	public class GameConfig : ScriptableObject
	{
		public AssetsRepository AssetsRepository;

		public GameObject GameInputPrefab;
		public MessageBox MessageBoxPrefab;
		public GameObject LoadingScreenPrefab;
		public GameObject SystemOverlayPrefab;

		public GridLevelController TowerLevelController;

		[Tooltip("Shape template to use along the playthroughs. Can be overridden by the playthrough template.")]
		public GridShapeTemplate[] ShapeTemplates;

		public BlocksSkinSet DefaultBlocksSet;

		public SceneReference BootScene;
		public SceneReference BootSceneMobile;

		public SceneReference WorldMapScene;
		public SceneReference WorldMapSceneMobile;

		public GameObject[] UIPrefabs;
		public GameObject[] UIPrefabsMobile;

		[Header("Gameplay")]
		public float FallSpeedup = 40f;

		[Header("Input")]
		public InputBindingDisplayAsset[] BindingDisplayAssets;

		public float SwipeMaxTime = 0.5f;
		public float SwipeMinDistance = 100f;
		public float AnalogMoveSpeed = 0.025f;
		public float AnalogRotateSpeed = 0.020f;

		[Range(0f, 1f)]
		public float SwipeConformity = 0.9f;

		[Header("Audio")]
		public float MultiplayerSpatialBlend = 0.4f;

		[Header("Templates")]
		public PlaythroughTemplateBase DevDefaultPlaythgrough;

		void OnValidate()
		{
			if (DefaultBlocksSet) {
				DefaultBlocksSet.Validate(AssetsRepository, this);
			} else {
				Debug.LogError($"No default block set for game config {name}", this);
			}
		}

#if UNITY_EDITOR
		public static GameConfig FindDefaultConfig() => UnityEditor.AssetDatabase.FindAssets($"t:{nameof(GameConfig)}")
					.Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
					.Select(UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>)
					.FirstOrDefault()
					;
#endif
	}
}