using TetrisTower.Core;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	[CreateAssetMenu(fileName = "UnknownGameSettings", menuName = "Tetris Tower/Game Settings")]
	public class GameControllerConfig : ScriptableObject
	{
		public AssetsRepository AssetsRepository;

		public GameObject GameInputPrefab;

		public float FallSpeedup = 40f;

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