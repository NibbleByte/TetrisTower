using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Playthrough for 2 or more players. Single level only.
	/// NOTE: <see cref="TowerLevel"/> will always return null.
	///		  Keep the level data returned by <see cref="SetupCurrentTowerLevel(GameConfig, SceneReference)"/> for each player.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PVPPlaythroughData : PlaythroughDataBase
	{
		public int PlayersCount = 2;

		public LevelParamData Level => m_LevelAsset?.LevelParam;

		[SerializeField]
		[Tooltip("Level to play.")]
		[JsonProperty]
		private WorldMapLevelParamAsset m_LevelAsset;

		public override bool IsFinalLevel => true;
		public override bool HaveFinishedLevels => true;

		private List<GridLevelData> m_TowerLevels = new List<GridLevelData>();

		public override ILevelSupervisor PrepareSupervisor()
		{
			return new TowerLevelSupervisor(this, PlayersCount);
		}

		public override GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (m_LevelAsset == null) {
				Debug.LogError($"No level asset set. Abort!");
				return null;
			}

			GridLevelData levelData = GenerateTowerLevelData(gameConfig, m_LevelAsset.LevelParam);

			if (overrideScene != null) {
				levelData.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {m_LevelAsset.name} - \"{levelData.BackgroundScene?.ScenePath}\".");

			m_TowerLevels.Add(levelData);

			return levelData;
		}

		public override void FinishLevel()
		{
			base.FinishLevel();
			// TODO:..
		}

		public override void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			base.Validate(repo, context);

			if (m_LevelAsset == null) {
				Debug.LogError($"{context} has no level asset specified.", context);
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(PVPPlaythroughData))]
	public class PVPPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<PVPPlaythroughData>
	{
	}
#endif
}