using DevLocker.GFrame;
using DevLocker.GFrame.Utils;
using DevLocker.Utils;
using Newtonsoft.Json;
using System;
using System.Linq;
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

		public override bool QuitLevelCanResumePlaythrough => m_ReturnPlaythrough != null;

		public override bool HaveFinishedLevels => m_ReturnPlaythrough == null;

		public ReplayPlaythroughData(ReplayRecording recording, IPlaythroughData returnPlaythrough)
		{
			m_PlaybackRecording = recording;
			m_ReturnPlaythrough = returnPlaythrough;

			// Just in case.
			foreach (var playerRecording in recording.PlayerRecordings) {
				playerRecording.GridLevelController = null;
				playerRecording.Fairy = null;
			}
		}

		public override ILevelSupervisor PrepareSupervisor()
		{
			return m_PlaybackRecording != null
				? new TowerLevelSupervisor(this, m_PlaybackRecording.PlayerRecordings.Length)
				: m_ReturnPlaythrough?.PrepareSupervisor() ?? new HomeScreen.HomeScreenLevelSupervisor();
		}

		public override GridLevelData SetupCurrentTowerLevel(GameConfig gameConfig, SceneReference overrideScene)
		{
			// Support replays with multiple players.
			//m_ActiveTowerLevels.Clear();

			m_ActiveTowerLevels.Add(Saves.SavesManager.Deserialize<GridLevelData>(m_PlaybackRecording.InitialState, gameConfig));

			return m_ActiveTowerLevels.Last();
		}

		public override void AssignPlayer(PlaythroughPlayer player, GridLevelData levelData)
		{
			base.AssignPlayer(player, levelData);

			player.LevelController.FinishedLevel += () => OnLevelFinished(player);
		}

		public ReplayRecording GetReplayRecording()
		{
			// Clone it just in case?
			var clone = m_PlaybackRecording.Clone();

			foreach(var playerRecording in clone.PlayerRecordings) {
				playerRecording.GridLevelController = null;
				playerRecording.Fairy = null;
			}

			return clone;
		}

		public ReplayActionsRecording GetPlayerRecording(int playerIndex, GridLevelController controller, Visuals.Effects.FairyMatchingController fairy)
		{
			// Clone it just in case?
			ReplayActionsRecording recording = m_PlaybackRecording.PlayerRecordings[playerIndex].Clone();
			recording.GridLevelController = controller;
			recording.Fairy = fairy;

			return recording;
		}

		public override void QuitLevel()
		{
			base.QuitLevel();

			m_PlaybackRecording = null;
		}

		public void OnReplayEnded(PlaythroughPlayer player)
		{
			// In case the replay ended without the level finishing (no winner).

			if (ActivePlayers.All(p => p.PlayerContext.StatesStack.Context.FindByType<LevelReplayPlayback>().PlayerPlaybackRecording.HasEnding)) {

				// First player has all the control - show his UI and hide the rest.
				player = ActivePlayers.First();

				player.RenderInputCanvasToScreen();

				foreach (var otherPlayer in ActivePlayers) {
					if (player != otherPlayer) {
						otherPlayer.HideCanvases();
					}
				}
			}
		}

		private void OnLevelFinished(PlaythroughPlayer player)
		{
			// Single player is handled by the level itself.
			if (ActivePlayers.Count() == 1)
				return;

			if (player.LevelData.HasWon) {

				// First player has all the control - show his UI and hide the rest.
				player = ActivePlayers.First();

				player.RenderInputCanvasToScreen();

				foreach (var otherPlayer in ActivePlayers) {
					if (player != otherPlayer) {
						otherPlayer.HideCanvases();
					}
				}

				return;
			}

			// Hide UI for lost players.
			player.HideCanvases();
		}
	}

#if UNITY_EDITOR
	[UnityEditor.CustomPropertyDrawer(typeof(ReplayPlaythroughData))]
	public class ReplayPlaythroughDataDrawer : SerializeReferenceCreatorDrawer<ReplayPlaythroughData>
	{
	}
#endif
}