using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;
using UnityEngine;

namespace TetrisTower.TowerLevels.Replays
{
	public enum ReplayActionType
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

		// Keep gameplay changing actions before Pause.
		Pause = 20,


		Cheat_EndLevel = 40,

		RecordingEnd = 99,
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

				// Don't pause during playback.
				// Advance the random sequence in case of pause to prevent easy modifying of the replay. And maybe "cheating".
				case ReplayActionType.Pause: levelController.LevelData.Random.Next(); break;

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
				_ => 0f,
			};
	}

	[JsonObject(MemberSerialization.Fields)]
	public class ReplayRecording
	{
		public const int CurrentRuntimeVersion = 1;

		public int Version;
		public bool IsVersionSupported => Version == CurrentRuntimeVersion;

		public IReadOnlyList<ReplayAction> Actions => m_Actions;
		private List<ReplayAction> m_Actions = new List<ReplayAction>();

		public bool HasEnding => m_Actions.LastOrDefault().ActionType == ReplayActionType.RecordingEnd;

		public string InitialState { get; private set; }
		public Vector3Int InitialFairyPos { get; private set; }
		public Vector3Int[] InitialFairyRestPoints { get; private set; }
		public int InitialVisualsRandomSeed { get; private set; }

		public string FinalState { get; private set; }

		private const int FloatMultiplier = 1000;

		[JsonIgnore]
		public GridLevelController GridLevelController;

		[JsonIgnore]
		public Visuals.Effects.FairyMatchingController Fairy;

		public ReplayRecording Clone()
		{
			ReplayRecording clone = MemberwiseClone() as ReplayRecording;
			clone.m_Actions = m_Actions.ToList();
			InitialFairyRestPoints = clone.InitialFairyRestPoints.ToArray();

			return clone;
		}

		public void AddAndRun(ReplayActionType actionType, float value = 0f)
		{
			if (HasEnding)
				throw new InvalidOperationException($"Trying to add action {actionType} after recording has ended is now allowed.");

			var action = new ReplayAction(actionType, value);

			// Execute before adding the action as it will also store the expected value.
			action.Replay(GridLevelController, Fairy);

			m_Actions.Add(action);

			if (!GridLevelController.LevelData.IsPlaying) {

				m_Actions.Add(new ReplayAction(ReplayActionType.RecordingEnd));

				FinalState = Saves.SavesManager.Serialize<GridLevelData>(GridLevelController.LevelData, GameManager.Instance.GameContext.GameConfig);
			}
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

			while(fairyRestPoints.Count > InitialFairyRestPoints.Length) {
				GameObject.DestroyImmediate(fairyRestPoints[fairyRestPoints.Count - 1]);
				fairyRestPoints.RemoveAt(fairyRestPoints.Count - 1);
			}

			for(int i = 0; i < InitialFairyRestPoints.Length; i++) {
				if (i < fairyRestPoints.Count) {
					fairyRestPoints[i].localPosition = InitialFairyRestPoints[i] / FloatMultiplier;
				} else {
					Transform additionalPoint = GameObject.Instantiate(fairyRestPoints[0].gameObject, fairyRestPoints[0].parent).transform;
					additionalPoint.localPosition = InitialFairyRestPoints[i] / FloatMultiplier;
					fairyRestPoints.Add(additionalPoint);
				}
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