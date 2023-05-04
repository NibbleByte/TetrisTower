using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// World play through with the world map.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_WorldPlaythroughTemplate", menuName = "Tetris Tower/World Playthrough Template")]
	public class WorldPlaythroughTemplate : PlaythroughTemplateBase
	{
		[SerializeField] private WorldPlaythroughData m_PlayerData;

		public override bool HasActiveLevel => m_PlayerData.TowerLevel != null;

		public override IPlaythroughData GeneratePlaythroughData(GameConfig config)
		{
			m_PlayerData.Validate(config.AssetsRepository, this);

			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(m_PlayerData, Saves.SaveManager.GetConverters(config));

			// No need to have the json "TypeNameHandling = Auto" of the root object serialized, as we specify the type in the generics parameter.
			return Newtonsoft.Json.JsonConvert.DeserializeObject<WorldPlaythroughData>(serialized, Saves.SaveManager.GetConverters(config));
		}

		public override IEnumerable<LevelParamData> GetAllLevels()
		{
			return m_PlayerData.LevelsSet.Levels.Where(la => la.LevelAsset != null).Select(la => la.LevelAsset.LevelParam);
		}


#if UNITY_EDITOR

		private void OnValidate()
		{
			if (UnityEditor.EditorApplication.isUpdating)
				return;

			if (m_PlayerData != null) {
				GameConfig gameConfig = GameConfig.FindDefaultConfig();
				m_PlayerData.Validate(gameConfig.AssetsRepository, this);
			}
		}
#endif
	}
}