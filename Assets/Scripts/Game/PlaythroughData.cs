using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Logic;
using TetrisTower.TowerLevels;
using UnityEngine;

namespace TetrisTower.Game
{
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class PlaythroughData
	{
		[SerializeReference] public GridLevelData TowerLevel;

		public int CurrentLevelIndex = 0;
		public LevelParamData[] Levels;

		public int ScoreOnLevelStart = 0;
		public int TotalScore => ScoreOnLevelStart + TowerLevel?.Score.Score ?? 0;

		public bool IsFinalLevel => CurrentLevelIndex == Levels.Length - 1;
		public bool HaveFinishedLevels => CurrentLevelIndex >= Levels.Length;

		public void FinishLevel()
		{
			ScoreOnLevelStart += TowerLevel.Score.Score;

			TowerLevel = null;
			CurrentLevelIndex++;
		}
	}

	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class LevelParamData
	{
		public SceneReference BackgroundScene;

		public GridShapeTemplate[] ShapeTemplates;
		public BlockType[] SpawnedBlocks;

		public GridRules Rules;

		[Range(5, 20)]
		public int GridRows = 13;

		// Validation value - cone visuals require different visual models for each size.
		// We don't support any other values for now.
		public const int SupportedColumnsCount = 13;
		[Range(SupportedColumnsCount, SupportedColumnsCount)]
		public int GridColumns = SupportedColumnsCount;

		public int ClearBlocksEndCount;

		public GridLevelData GenerateTowerLevelData()
		{
			var levelData = new GridLevelData() {
				BackgroundScene = BackgroundScene?.Clone() ?? new SceneReference(),

				ShapeTemplates = ShapeTemplates.ToArray(),
				SpawnedBlocks = SpawnedBlocks.ToArray(),

				Rules = Rules,
				Grid = new BlocksGrid(GridRows, GridColumns),

				ClearBlocksEndCount = ClearBlocksEndCount,
			};

			return levelData;
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(PlaythroughData))]
	public class PlaythroughDataDrawer : Tools.SerializeReferenceCreatorDrawer<PlaythroughData>
	{
	}
#endif
}