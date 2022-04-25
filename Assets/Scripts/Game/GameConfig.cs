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

		public GridLevelController TowerLevelController;

		[Tooltip("Shape template to use along the playthroughs. Can be overridden by the playthrough template.")]
		public GridShapeTemplate[] ShapeTemplates;

		[Tooltip("Blocks to be used along the playthroughs. The index decides the order of appearance. Can be overridden by the playthrough template.")]
		public BlockType[] Blocks;

		[Tooltip("Block to be used for bonus blocks in the won animation at the end.")]
		public BlockType WonBonusBlock;

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
				new RandomXoShiRo128starstarJsonConverter(),
		};

		public PlaythroughTemplate NormalPlaythgrough;

		void OnValidate()
		{
			for (int i = 0; i < Blocks.Length; i++) {
				var block = Blocks[i];

				if (block == null) {
					Debug.LogError($"Missing {i}-th block from {nameof(Blocks)} in {nameof(GameConfig)} at {name}", this);
					continue;
				}

				if (!AssetsRepository.IsRegistered(block)) {
					Debug.LogError($"Block {block.name} is not registered for serialization, from {nameof(Blocks)} in {nameof(GameConfig)} at {name}", this);
				}
			}
		}
	}
}