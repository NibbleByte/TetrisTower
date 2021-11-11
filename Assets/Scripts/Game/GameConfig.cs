using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.Input.UIInputDisplay;
using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownGameSettings", menuName = "Tetris Tower/Game Config")]
	public class GameConfig : ScriptableObject
	{
		public AssetsRepository AssetsRepository;

		public GameObject GameInputPrefab;
		public MessageBox MessageBoxPrefab;

		public const string TowerPlaceholderTag = "TowerPlaceholder";
		public TowerLevels.TowerLevelController TowerLevelController;

		public GameObject[] UIPrefabs;

		public InputBindingDisplayAsset[] BindingDisplayAssets;

		public float FallSpeedup = 40f;

		public float SwipeMaxTime = 0.5f;
		public float SwipeMinDistance = 100f;
		public float AnalogMoveSpeed = 0.025f;
		public float AnalogRotateSpeed = 0.020f;

		[Range(0f, 1f)]
		public float SwipeConformity = 0.9f;

		public Newtonsoft.Json.JsonConverter[] Converters => new Newtonsoft.Json.JsonConverter[] {
				new BlockTypeConverter(AssetsRepository),
				new GridShapeTemplateConverter(AssetsRepository),
		};

		public PlaythroughTemplate NormalPlaythgrough;
	}
}