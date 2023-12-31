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
		public int AttackChargeMax = 40;

		public LevelParamData Level => m_LevelAsset?.LevelParam;

		[SerializeField]
		[Tooltip("Level to play.")]
		[JsonProperty]
		private WorldMapLevelParamAsset m_LevelAsset;

		public override bool IsFinalLevel => true;
		public override bool HaveFinishedLevels => true;

		private Dictionary<PlaythroughPlayer, int> m_AttacksCharge = new Dictionary<PlaythroughPlayer, int>();

		public float GetAttackChargeNormalized(PlaythroughPlayer player) => Mathf.Clamp01(m_AttacksCharge[player] / (float)AttackChargeMax);

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

			m_AttacksCharge.Add(player, 0);
		}

		protected override void DisposeTowerLevels()
		{
			base.DisposeTowerLevels();

			m_AttacksCharge.Clear();
		}

		private void OnClearActionSequenceFinished(PlaythroughPlayer player)
		{
			int charge = m_AttacksCharge[player] + player.LevelData.Score.LastMatchBonus.ResultBonusScore;
			if (charge < AttackChargeMax) {
				m_AttacksCharge[player] = charge;

			} else {

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
					m_AttacksCharge[player] = 0;

					bestEnemy.LevelData.PendingBonusActions.Add(new PushUpLine_BonusAction());
				}
			}
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