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
using UnityEngine.Serialization;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Sequential play through in which players play levels one after another until end is reached.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class SeqPlaythroughData : PlaythroughDataBase
	{
		public int CurrentLevelIndex = 0;

		public LevelParamData[] Levels => m_Levels.Length > 0 ? m_Levels : m_LevelAssets.Select(la => la ? la.LevelParam : null).ToArray();

		[SerializeField]
		[FormerlySerializedAs("Levels")]
		[Tooltip("List of levels to use defined in place. Will override the LevelAssets.")]
		private LevelParamData[] m_Levels = new LevelParamData[0];

		[SerializeField]
		[Tooltip("List of level assets to use. Will be overridden by Levels if any.")]
		private WorldMapLevelParamAsset[] m_LevelAssets = new WorldMapLevelParamAsset[0];

		public override bool IsFinalLevel => CurrentLevelIndex == Levels.Length - 1;
		public override bool HaveFinishedLevels => CurrentLevelIndex >= Levels.Length;

		public override ILevelSupervisor PrepareSupervisor()
		{
			return new TowerLevelSupervisor(this);
		}

		public void ReplaceLevelParams(IEnumerable<LevelParamData> overrideParams)
		{
			if (ActiveTowerLevels.Count > 0 || ActivePlayers.Any())
				throw new InvalidOperationException("Cannot replace level params when tower level is in progress.");

			m_Levels = overrideParams.ToArray();
			CurrentLevelIndex = 0;
		}

		public override GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (CurrentLevelIndex >= Levels.Length) {
				Debug.LogError($"Current level {CurrentLevelIndex} is out of range {Levels.Length}. Abort!");
				m_ActiveTowerLevels.Clear();
				return null;
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

			m_ActiveTowerLevels.Clear();
			m_ActiveTowerLevels.Add(GenerateTowerLevelData(gameConfig, Levels[CurrentLevelIndex]));

			if (overrideScene != null) {
				m_ActiveTowerLevels[0].BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {CurrentLevelIndex} - \"{m_ActiveTowerLevels[0].BackgroundScene?.ScenePath}\".");

			return m_ActiveTowerLevels[0];
		}

		public override void FinishLevel()
		{
			base.FinishLevel();

			CurrentLevelIndex++;
		}

		public override void Validate(Core.AssetsRepository repo, UnityEngine.Object context)
		{
			base.Validate(repo, context);

			if (m_Levels.Length > 0 && m_LevelAssets.Length > 0) {
				Debug.LogError($"{context} has both level and level assets.", context);
			}

			bool hasChanges = false;
			HashSet<Logic.Objective> allObjectives = new HashSet<Logic.Objective>();

			foreach (var level in Levels) {
				if (level != null) {
					level.Validate(repo, context);

					for(int i = 0; i < level.Objectives.Count; ++i) {
						var objective = level.Objectives[i];

						// Levels sharing the same objective reference is not allowed. This is most likely bad setup in Unity.
						if (objective != null && !allObjectives.Add(objective)) {
							level.Objectives.RemoveAt(i);
							i--;
							hasChanges = true;
						}
					}

				} else {
					Debug.LogError($"{context} has missing levels.", context);
				}

#if UNITY_EDITOR
				if (hasChanges) {
					UnityEditor.EditorUtility.SetDirty(context);
				}
#endif
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