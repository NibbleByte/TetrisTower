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

		public void RetryLevel()
		{
			if (TowerLevel == null)
				throw new InvalidOperationException($"Trying to retry a level when no level is in progress.");

			TowerLevel = null;
		}

		public void FinishLevel()
		{
			if (TowerLevel == null || TowerLevel.RunningState != TowerLevelRunningState.Won)
				throw new InvalidOperationException($"Trying to finish level when it hasn't finished - {TowerLevel?.RunningState}.");

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

		[Tooltip("Leave the seed to 0 for random seed every time.")]
		public int RandomSeed = 0;

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
			int seed = RandomSeed != 0 ? RandomSeed : UnityEngine.Random.Range(0, int.MaxValue);

			var levelData = new GridLevelData() {
				BackgroundScene = BackgroundScene?.Clone() ?? new SceneReference(),

				ShapeTemplates = ShapeTemplates.ToArray(),
				SpawnedBlocks = SpawnedBlocks.ToArray(),

				RandomInitialSeed = seed,
				Random = new System.Random(seed),

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