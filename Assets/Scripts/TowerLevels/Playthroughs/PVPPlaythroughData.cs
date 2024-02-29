using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Replays;
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
		public int AttackChargeMax = 40;

		public LevelParamData Level => m_LevelAsset?.LevelParam;

		[SerializeField]
		[Tooltip("Level to play.")]
		[JsonProperty]
		private WorldMapLevelParamAsset m_LevelAsset;

		// This somehow remains set, even after assembly reload. Ignore serialization in any way.
		[JsonIgnore]
		[NonSerialized]
		private LevelParamData m_OverrideLevelParam;

		public override bool IsFinalLevel => true;
		public override bool HaveFinishedLevels => true;

		public override ILevelSupervisor PrepareSupervisor()
		{
			return new TowerLevelSupervisor(this, PlayersCount);
		}

		public void ReplaceLevelParams(IEnumerable<LevelParamData> overrideParams)
		{
			if (ActiveTowerLevels.Count > 0 || ActivePlayers.Any())
				throw new InvalidOperationException("Cannot replace level params when tower level is in progress.");

			// Single level only supported at the moment.
			m_OverrideLevelParam = overrideParams.First();
			m_LevelAsset = null;
		}

		public override GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			if (m_LevelAsset == null && m_OverrideLevelParam == null) {
				Debug.LogError($"No level asset set. Abort!");
				return null;
			}

			GridLevelData levelData = GenerateTowerLevelData(gameConfig, m_OverrideLevelParam ?? m_LevelAsset.LevelParam);

			// All levels should start with the same seed.
			if (m_ActiveTowerLevels.Count > 0) {
				levelData.RandomInitialLevelSeed = m_ActiveTowerLevels[0].RandomInitialLevelSeed;
				levelData.Random = new Xoshiro.PRNG32.XoShiRo128starstar(levelData.RandomInitialLevelSeed);
			}

			if (overrideScene != null) {
				levelData.BackgroundScene = overrideScene;
			}

			Debug.Log($"Setup current level {m_OverrideLevelParam?.BackgroundScene?.SceneName ?? m_LevelAsset.name} - \"{levelData.BackgroundScene?.ScenePath}\".");

			m_ActiveTowerLevels.Add(levelData);

			return levelData;
		}

		public override void AssignPlayer(PlaythroughPlayer player, GridLevelData levelData)
		{
			base.AssignPlayer(player, levelData);

			if (levelData.Score == null) {
				levelData.Score = new ScoreGrid(levelData.Grid.Rows, levelData.Grid.Columns, levelData.Rules);
			}

			// No need to unsubscribe, as instance will be lost on level unload?
			levelData.Score.ClearActionSequenceFinished += () => OnClearActionSequenceFinished(player);

			levelData.AttackCharge = 0;
			levelData.AttackChargeMax = AttackChargeMax;
		}

		protected override void DisposeTowerLevels()
		{
			base.DisposeTowerLevels();

			m_OverrideLevelParam = null;
		}

		private void OnClearActionSequenceFinished(PlaythroughPlayer player)
		{
			var recording = player.PlayerContext.StatesStack.Context.FindByType<ReplayActionsRecording>();

			// Won bonus blocks falling down. Skip them.
			if (!player.LevelData.IsPlaying || recording.HasEnding)
				return;

			recording.AddAndRun(ReplayActionType.ChargeAttack, player.LevelData.Score.LastMatchBonus.ResultBonusScore);

			if (player.LevelData.AttackCharge >= AttackChargeMax) {

				// Pick who to attack.
				PlaythroughPlayer bestEnemy = null;
				foreach(PlaythroughPlayer enemy in ActivePlayers) {
					if (enemy == player)
						continue;

					if (bestEnemy == null || enemy.LevelData.Score.Score > bestEnemy.LevelData.Score.Score) {
						bestEnemy = enemy;
					}
				}

				// In debug we can run 1 player, so no best enemy would be found?
				if (bestEnemy != null) {
					recording.AddAndRun(ReplayActionType.ConsumeAttackCharge);

					var bestRecording = bestEnemy.PlayerContext.StatesStack.Context.FindByType<ReplayActionsRecording>();
					bestRecording.AddAndRun(ReplayActionType.PushUpLine_Attack, 0);
				}
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