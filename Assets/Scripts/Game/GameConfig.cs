using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.UIInputDisplay;
using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownGameSettings", menuName = "Tetris Tower/Game Settings")]
	public class GameConfig : ScriptableObject
	{
		public AssetsRepository AssetsRepository;

		public GameObject GameInputPrefab;
		public MessageBox MessageBoxPrefab;

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

	[SerializeField] private PlaythroughData m_NewGameData;
		public PlaythroughData NewGameData
		{
			get {
				var converters = new Newtonsoft.Json.JsonConverter[] {
					new BlockTypeConverter(AssetsRepository),
					new GridShapeTemplateConverter(AssetsRepository),
				};

				// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
				var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(m_NewGameData, converters);

				return Newtonsoft.Json.JsonConvert.DeserializeObject<PlaythroughData>(serialized, converters);
			}
		}

	}
}