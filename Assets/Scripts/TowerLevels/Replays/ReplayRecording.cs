using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels.Replays
{
	public enum ReplayActionType : byte
	{
		Update = 0,

		FairyPos = 2,

		Move = 4,
		Rotate = 5,

		OffsetMove = 8,
		OffsetRotate = 9,

		ClearMoveOffset = 12,
		ClearRotateOffset = 13,

		FallSpeedUp = 16,

		ChargeAttack = 30,			// Players accumulate attack charge
		ConsumeAttackCharge = 32,	// When players perform the attack, it consumes (resets) the charge to 0.

		PushUpLine_Attack = 35,		// Attack that happens to the target player, recorded in their replay.

		OtherPlayersLost = 50,		// When other players lost, this is recorded by the winner.

		// Keep gameplay changing actions before Pause.
		Pause = 100,



		Cheat_Generic = 150,
		Cheat_EndLevel = 154,

		RecordingEnd = byte.MaxValue,
	}

	[JsonObject(MemberSerialization.Fields)]
	public struct ReplayAction
	{
		public ReplayActionType ActionType;
		public float Value;

		/// <summary>
		/// Expected value AFTER the new one is applied.
		/// </summary>
		public float ExpectedResultValue;

		public ReplayAction(ReplayActionType actionType, float value = 0)
		{
			ActionType = actionType;
			Value = value;
			ExpectedResultValue = 0f;
		}

		public override string ToString() => $"{ActionType} {Value} {ExpectedResultValue}";


		public void Replay(GridLevelController levelController, Visuals.Effects.FairyMatchingController fairy)
		{
			switch (ActionType) {
				case ReplayActionType.Update: levelController.Timing.UpdateCoroutines(Value); break;

				case ReplayActionType.FairyPos: fairy.LevelUpdate(Value); break;

				case ReplayActionType.Move: levelController.RequestFallingShapeMove((int) Value); break;
				case ReplayActionType.Rotate: levelController.RequestFallingShapeRotate((int) Value); break;

				case ReplayActionType.OffsetMove: levelController.AddFallingShapeAnalogMoveOffset(Value); break;
				case ReplayActionType.OffsetRotate: levelController.AddFallingShapeAnalogRotateOffset(Value); break;

				case ReplayActionType.ClearMoveOffset: levelController.ClearFallingShapeAnalogMoveOffset(); break;
				case ReplayActionType.ClearRotateOffset: levelController.ClearFallingShapeAnalogRotateOffset(); break;

				case ReplayActionType.FallSpeedUp: levelController.RequestFallingSpeedUp(Value); break;

				// PVP
				case ReplayActionType.ChargeAttack:
					levelController.LevelData.AttackCharge += (int)Value;
					break;
				case ReplayActionType.ConsumeAttackCharge:
					levelController.LevelData.AttackCharge = 0;
					break;
				case ReplayActionType.PushUpLine_Attack:
					levelController.LevelData.PendingBonusActions.Add(new PushUpLine_BonusAction());
					break;

				case ReplayActionType.OtherPlayersLost: levelController.FinishLevel(true); break;

				// Don't pause during playback.
				// Advance the random sequence in case of pause to prevent easy modifying of the replay. And maybe "cheating".
				case ReplayActionType.Pause: levelController.LevelData.Random.Next(); break;

				// Similarly to Pause, advance the random sequence, to prevent "cheating".
				case ReplayActionType.Cheat_Generic: levelController.LevelData.Random.Next(); break;

				case ReplayActionType.Cheat_EndLevel: levelController.FinishLevel(Value != 0); break;

				case ReplayActionType.RecordingEnd: /* Do nothing, used as marker. */ break;

				default: throw new NotSupportedException(ActionType.ToString());
			}

			ExpectedResultValue = GetExpectedResultValue(levelController, fairy);
		}

		public float GetExpectedResultValue(GridLevelController levelController, Visuals.Effects.FairyMatchingController fairy) =>
			ActionType switch {
				ReplayActionType.Update => levelController.LevelData.FallDistanceNormalized,
				ReplayActionType.FairyPos => fairy.ReplayReferenceValue,

				ReplayActionType.Move => levelController.LevelData.FallingColumn,

				ReplayActionType.OffsetMove => levelController.FallingColumnAnalogOffset,
				ReplayActionType.OffsetRotate => levelController.FallingShapeAnalogRotateOffset,

				ReplayActionType.ChargeAttack => levelController.LevelData.AttackCharge,
				ReplayActionType.ConsumeAttackCharge => levelController.LevelData.AttackCharge,
				ReplayActionType.PushUpLine_Attack => levelController.LevelData.PendingBonusActions.Count,

				_ => 0f,
			};
	}

	/// <summary>
	/// Recording that includes the initial state and recorded actions for each player.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public class ReplayRecording
	{
		public const int CurrentRuntimeVersion = 1;

		public int Version;
		public bool IsVersionSupported => Version == CurrentRuntimeVersion;

		[field: JsonProperty(nameof(InitialState))]
		public string InitialState { get; private set; }

		[field: JsonProperty(nameof(InitialFairyPos))]
		public Vector3Int InitialFairyPos { get; private set; }

		[field: JsonProperty(nameof(InitialFairyRestPoints))]
		public Vector3Int[] InitialFairyRestPoints { get; private set; }

		[field: JsonProperty(nameof(InitialVisualsRandomSeed))]
		public int InitialVisualsRandomSeed { get; private set; }

		[field: JsonProperty(nameof(PlayerRecordings))]
		public ReplayActionsRecording[] PlayerRecordings { get; private set; }

		private const int FloatMultiplier = 1000;

		public static ReplayRecording CreateRecording(int players) => new ReplayRecording() {
			PlayerRecordings = Enumerable.Range(0, players).Select(index => new ReplayActionsRecording()).ToArray(),
		};

		public ReplayRecording Clone()
		{
			ReplayRecording clone = (ReplayRecording) MemberwiseClone();
			clone.InitialFairyRestPoints = InitialFairyRestPoints.ToArray();
			clone.PlayerRecordings = PlayerRecordings.Select(r => r.Clone()).ToArray();

			return clone;
		}

		public void SaveInitialState(GridLevelData levelData, GameConfig gameConfig, Visuals.Effects.FairyMatchingController fairy, List<Transform> fairyRestPoints, int initialVisualsRandomSeed)
		{
			InitialState = Saves.SavesManager.Serialize<GridLevelData>(levelData, gameConfig);
			InitialFairyPos = Vector3Int.FloorToInt(fairy.transform.localPosition * FloatMultiplier);
			InitialFairyRestPoints = fairyRestPoints.Select(t => Vector3Int.FloorToInt(t.localPosition * FloatMultiplier)).ToArray();
			InitialVisualsRandomSeed = initialVisualsRandomSeed;

			Version = CurrentRuntimeVersion;

			ApplyFairyPositions(fairy, fairyRestPoints);
		}

		public void ApplyFairyPositions(Visuals.Effects.FairyMatchingController fairy, List<Transform> fairyRestPoints)
		{
			fairy.transform.localPosition = InitialFairyPos / FloatMultiplier;

			while (fairyRestPoints.Count > InitialFairyRestPoints.Length) {
				GameObject.DestroyImmediate(fairyRestPoints[fairyRestPoints.Count - 1]);
				fairyRestPoints.RemoveAt(fairyRestPoints.Count - 1);
			}

			for (int i = 0; i < InitialFairyRestPoints.Length; i++) {
				if (i < fairyRestPoints.Count) {
					fairyRestPoints[i].localPosition = InitialFairyRestPoints[i] / FloatMultiplier;
				} else {
					Transform additionalPoint = GameObject.Instantiate(fairyRestPoints[0].gameObject, fairyRestPoints[0].parent).transform;
					additionalPoint.localPosition = InitialFairyRestPoints[i] / FloatMultiplier;
					fairyRestPoints.Add(additionalPoint);
				}
			}
		}
	}

	/// <summary>
	/// Recorded actions for a signle player.
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public class ReplayActionsRecording
	{
		public IReadOnlyList<ReplayAction> Actions => m_Actions;
		[field: JsonProperty(nameof(Actions))]
		private List<ReplayAction> m_Actions = new List<ReplayAction>();

		public bool HasEnding => m_Actions.LastOrDefault().ActionType == ReplayActionType.RecordingEnd;

		[field: JsonProperty(nameof(FinalState))]
		public string FinalState { get; private set; }

		[JsonIgnore]
		public GridLevelController GridLevelController;

		[JsonIgnore]
		public Visuals.Effects.FairyMatchingController Fairy;

		[JsonIgnore]
		private bool m_AddAndRunInProgress = false;

		public ReplayActionsRecording Clone()
		{
			ReplayActionsRecording clone = (ReplayActionsRecording) MemberwiseClone();
			clone.m_Actions = m_Actions.ToList();

			return clone;
		}

		public void AddAndRun(ReplayActionType actionType, float value = 0f, bool forceAdd = false)
		{
			if (HasEnding)
				throw new InvalidOperationException($"Trying to add action {actionType} after recording has ended is not allowed.");

			var action = new ReplayAction(actionType, value);

			// If running action causes another action to be added - just run it. Replay should produce the same result so no need to record it.
			// ForceAdd will skip this and add it anyway. Use it when the replay level won't produce this action on its own (e.g. PVP playthrough logic).
			if (m_AddAndRunInProgress && !forceAdd) {
				action.Replay(GridLevelController, Fairy);
				return;
			}

			m_AddAndRunInProgress = true;

			// Execute before adding the action as it will also store the expected value.
			action.Replay(GridLevelController, Fairy);

			m_Actions.Add(action);

			m_AddAndRunInProgress = false;

			if (!GridLevelController.LevelData.IsPlaying) {

				m_Actions.Add(new ReplayAction(ReplayActionType.RecordingEnd));

				FinalState = Saves.SavesManager.Serialize<GridLevelData>(GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);
			}
		}

		public void EndReplayRecording()
		{
			if (HasEnding)
				throw new InvalidOperationException("Trying to end replay recording, when it is already ended.");

			AddAndRun(ReplayActionType.RecordingEnd);

			FinalState = Saves.SavesManager.Serialize<GridLevelData>(GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);
		}

		public string GetSavedState(GridLevelData levelData, GameConfig gameConfig)
		{
			return Saves.SavesManager.Serialize<GridLevelData>(levelData, gameConfig);
		}
	}
}