using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using TetrisTower.Game;
using TetrisTower.Logic;
using TetrisTower.TowerLevels.Replays;

namespace TetrisTower.TowerLevels.Playthroughs
{
	/// <summary>
	/// Playthrough used for replays. It feeds replay recording to the tower level.
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.Fields)]
	public class ReplayPlaythroughData : PlaythroughDataBase
	{
		private ReplayRecording m_PlaybackRecording;
		private readonly IPlaythroughData m_ReturnPlaythrough;

		public override bool IsFinalLevel => m_ReturnPlaythrough == null;

		public override bool HaveFinishedLevels => m_ReturnPlaythrough == null;

		public ReplayPlaythroughData(ReplayRecording recording, IPlaythroughData returnPlaythrough)
		{
			m_PlaybackRecording = recording;
			m_ReturnPlaythrough = returnPlaythrough;

			m_PlaybackRecording.GridLevelController = null; // Just in case.
		}

		public override ILevelSupervisor PrepareSupervisor()
		{
			return m_PlaybackRecording != null
				? new TowerLevelSupervisor(this)
				: m_ReturnPlaythrough?.PrepareSupervisor() ?? new HomeScreen.HomeScreenLevelSupervisor();
		}

		public override void SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			m_TowerLevel = Saves.SavesManager.Deserialize<GridLevelData>(m_PlaybackRecording.InitialState, gameConfig);
		}

		public ReplayRecording GetReplayRecording(GridLevelController controller)
		{
			// Clone it just in case?
			ReplayRecording recording = m_PlaybackRecording.Clone();
			recording.GridLevelController = controller;

			return recording;
		}

		public override void QuitLevel()
		{
			base.QuitLevel();

			m_PlaybackRecording = null;
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(ReplayPlaythroughData))]
	public class ReplayPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<ReplayPlaythroughData>
	{
	}
#endif
}