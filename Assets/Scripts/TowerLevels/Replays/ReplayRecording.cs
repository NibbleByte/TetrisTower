using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.Logic;

namespace TetrisTower.TowerLevels.Replays
{
	public enum ReplayActionType
	{
		Update = 0,

		Move = 4,
		Rotate = 5,

		OffsetMove = 8,
		OffsetRotate = 9,

		ClearMoveOffset = 12,
		ClearRotateOffset = 13,

		FallSpeedUp = 16,

		Pause = 20,
	}

	[JsonObject(MemberSerialization.Fields)]
	public struct ReplayAction
	{
		public ReplayActionType ActionType;
		public float Value;

		public ReplayAction(ReplayActionType actionType)
		{
			ActionType = actionType;
			Value = 0;
		}

		public ReplayAction(ReplayActionType actionType, float value)
		{
			ActionType = actionType;
			Value = value;
		}


		public void Replay(GridLevelController levelController)
		{
			switch (ActionType) {
				case ReplayActionType.Update: levelController.Timing.UpdateCoroutines(Value); break;

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

				default: throw new NotSupportedException(ActionType.ToString());
			}
		}
	}

	[JsonObject(MemberSerialization.Fields)]
	public class ReplayRecording
	{
		public IReadOnlyList<ReplayAction> Actions => m_Actions;
		private List<ReplayAction> m_Actions = new List<ReplayAction>();

		public string InitialState { get; private set; }
		public string FinalState { get; private set; }

		// TODO: Version.
		// TODO: Record fairy state.

		[JsonIgnore]
		public GridLevelController GridLevelController;

		public ReplayRecording Clone()
		{
			ReplayRecording clone = MemberwiseClone() as ReplayRecording;
			clone.m_Actions = m_Actions.ToList();

			return clone;
		}

		public void AddAction(ReplayAction action) => m_Actions.Add(action);
		public void AddAndRun(ReplayAction action)
		{
			m_Actions.Add(action);
			action.Replay(GridLevelController);
		}

		public void SaveInitialState(GridLevelData levelData, GameConfig gameConfig)
		{
			InitialState = Saves.SaveManager.Serialize<GridLevelData>(levelData, gameConfig);
		}

		public void SaveFinalState(GridLevelData levelData, GameConfig gameConfig)
		{
			FinalState = Saves.SaveManager.Serialize<GridLevelData>(levelData, gameConfig);
		}

		public string GetSavedState(GridLevelData levelData, GameConfig gameConfig)
		{
			return Saves.SaveManager.Serialize<GridLevelData>(levelData, gameConfig);
		}
	}
}