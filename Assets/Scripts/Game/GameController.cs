using TetrisTower.Core;
using TetrisTower.Levels;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.Game
{
	public class GameController : MonoBehaviour
	{
		public AssetsRepository AssetsRepository;

		// TODO: REMOVE
		public LevelData StartData;

		public static GameController Instance { get; private set; }

		public LevelController LevelController { get; private set; }

		public Newtonsoft.Json.JsonConverter[] Converters { get; private set; }

		void Awake()
		{
			if (Instance) {
				gameObject.SetActive(false);
			}

			Instance = this;

			Converters = new Newtonsoft.Json.JsonConverter[] {
				new BlockTypeConverter(AssetsRepository),
				new GridShapeTemplateConverter(AssetsRepository),
			};
		}

		void Start()
		{
			// For Debug
			var levelController = FindObjectOfType<LevelController>();
			if (levelController) {
				InitializeLevel(levelController, StartData);
			}
		}

		private void InitializeLevel(LevelController level, LevelData data)
		{
			LevelController = level;
			LevelController.Init(data);
		}

		private void OnDestroy()
		{
			if (Instance == this) {
				Instance = null;
			}
		}

		#region Debug Stuff

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F5)) {
				Serialize();
			}
			if (Input.GetKeyDown(KeyCode.F6)) {
				Deserialize();
			}
		}

		string m_DebugSave;
		void Serialize()
		{
			m_DebugSave = Newtonsoft.Json.JsonConvert.SerializeObject(LevelController.LevelData, Converters);
			Debug.Log(m_DebugSave);
		}

		void Deserialize()
		{
			if (string.IsNullOrEmpty(m_DebugSave))
				return;

			StartData = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelData>(m_DebugSave, Converters);
			Start();
		}

		#endregion
	}
}