using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.MessageBox;
using DevLocker.GFrame.Pools;
using System;
using TetrisTower.Game;
using TetrisTower.SystemUI;
using TetrisTower.TowerLevels.Playthroughs;
using TetrisTower.TowerLevels.Replays;
using UnityEngine;

namespace TetrisTower.HomeScreen
{
	public class HomeScreenReplaysController : MonoBehaviour, ILevelLoadedListener
	{
		public Transform ReplaysListContainer;

		private GameConfig m_Config;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_Config);
		}

		public void OnLevelUnloading()
		{
		}

		void OnEnable()
		{
			InPlacePools.TransformPool(ReplaysListContainer, 0);

			FetchReplaysAsync();
		}

		private async void FetchReplaysAsync()
		{
			string[] replayNames = await Saves.SavesManager.FetchReplaysList();

			// Changed state/screen.
			if (!gameObject.activeInHierarchy)
				return;

			InPlacePools.TransformPool(ReplaysListContainer, replayNames.Length);

			for(int i = 0; i < replayNames.Length; i++) {
				var entry = ReplaysListContainer.GetChild(i).GetComponent<HomeScreenReplaysEntryElement>();

				entry.Init(replayNames[i], OnPlay, OnDelete);
			}
		}

		private async void OnPlay(HomeScreenReplaysEntryElement replayEntry)
		{
			using (GameManager.Instance.GetManager<BlockingOperationOverlayController>().BlockScope(replayEntry)) {

				try {

					ReplayRecording recording = await Saves.SavesManager.LoadReplay(replayEntry.ReplayName, m_Config);

					var nextPlaythroughData = new ReplayPlaythroughData(recording, null);

					GameManager.Instance.SwitchLevelAsync(nextPlaythroughData.PrepareSupervisor());

				} catch (Exception ex) {

					Debug.LogException(ex);

					MessageBox.Instance.ShowSimple("Load Failed", $"Replay failed to load!", MessageBoxIcon.Error, MessageBoxButtons.OK, (Action)null);
				}
			}
		}

		private async void OnDelete(HomeScreenReplaysEntryElement replayEntry)
		{
			MessageBoxResponseData response = await MessageBox.Instance.ShowSimple("Delete Replay?", $"Are you sure you want to delete replay \"{replayEntry.ReplayName}\"?", MessageBoxIcon.Question, MessageBoxButtons.YesNo, (Action)null).WaitResponseAsync();
			if (response.DenyResponse)
				return;

			replayEntry.gameObject.SetActive(false);

			using (GameManager.Instance.GetManager<BlockingOperationOverlayController>().BlockScope(replayEntry)) {

				try {

					await Saves.SavesManager.DeleteReplay(replayEntry.ReplayName);

				} catch (Exception ex) {

					Debug.LogException(ex);

					MessageBox.Instance.ShowSimple("Delete Failed", $"Replay failed to delete!", MessageBoxIcon.Error, MessageBoxButtons.OK, (Action)null);
				}
			}
		}
	}

}