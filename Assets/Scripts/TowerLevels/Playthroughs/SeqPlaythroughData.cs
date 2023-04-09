using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels;
using UnityEngine;

namespace TetrisTower.TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class SeqPlaythroughData : PlaythroughDataBase
	{
		public int CurrentLevelIndex = 0;
		public LevelParamData[] Levels;

		public override bool IsFinalLevel => CurrentLevelIndex == Levels.Length - 1;
		public override bool HaveFinishedLevels => CurrentLevelIndex >= Levels.Length;

		public override ILevelSupervisor PrepareSupervisor()
		{
			return new TowerLevelSupervisor(this);
		}

		public void SetupCurrentLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (CurrentLevelIndex >= Levels.Length) {
				Debug.LogError($"Current level {CurrentLevelIndex} is out of range {Levels.Length}. Abort!");
				m_TowerLevel = null;
				return;
			}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// Start the level with the same scene.
			if (overrideScene != null) {
				int foundIndex = Array.FindIndex(Levels, l => l.BackgroundScene.ScenePath == overrideScene.ScenePath);
				if (foundIndex != -1) {
					CurrentLevelIndex = foundIndex;
				}
			}
#endif

			m_TowerLevel = GenerateTowerLevelData(gameConfig, Levels[CurrentLevelIndex]);

			if (overrideScene != null) {
				TowerLevel.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {CurrentLevelIndex} - \"{m_TowerLevel.BackgroundScene?.ScenePath}\".");
		}

		public override void FinishLevel()
		{
			base.FinishLevel();

			CurrentLevelIndex++;
		}

		public override void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			base.Validate(repo, context);

			foreach (var level in Levels) {
				level.Validate(repo, context);
			}
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(SeqPlaythroughData))]
	public class SeqPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<SeqPlaythroughData>
	{
	}
#endif
}