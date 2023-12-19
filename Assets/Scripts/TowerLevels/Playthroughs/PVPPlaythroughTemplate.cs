using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[CreateAssetMenu(fileName = "Unknown_PVPPlaythroughTemplate", menuName = "Tetris Tower/PVP Playthrough Template")]
	public class PVPPlaythroughTemplate : PlaythroughTemplateBase
	{
		[SerializeField] private PVPPlaythroughData m_PlayerData;

		public override bool HasActiveLevel => m_PlayerData.ActiveTowerLevels.Any();

		public override IPlaythroughData GeneratePlaythroughData(GameConfig config)
		{
			m_PlayerData.Validate(config.AssetsRepository, this);

			// Clone the instance instead of referring it directly, leaking changes into the scriptable object.
			// Specify the interface type so it writes down the root type name. Check out TypeNameHandling.Auto documentation
			return Saves.SavesManager.Clone<IPlaythroughData, PVPPlaythroughData>(m_PlayerData, config);
		}

		public override IEnumerable<LevelParamData> GetAllLevels()
		{
			yield return m_PlayerData.Level;
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