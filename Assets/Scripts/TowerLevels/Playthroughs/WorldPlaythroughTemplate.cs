using System;
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

		public override bool HasActiveLevel => m_PlayerData.ActiveTowerLevels.Any();

		public override IPlaythroughData GeneratePlaythroughData(GameConfig config, IEnumerable<LevelParamData> overrideParams = null)
		{
			if (overrideParams != null)
				throw new NotSupportedException("World playthrough doesn't support override level params as it works with set assets.");

			m_PlayerData.Validate(config.AssetsRepository, this);

			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			return Saves.SavesManager.Clone<IPlaythroughData, WorldPlaythroughData>(m_PlayerData, config);
		}

		public override IEnumerable<LevelParamData> GetAllLevels() => m_PlayerData.GetAllLevels();


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