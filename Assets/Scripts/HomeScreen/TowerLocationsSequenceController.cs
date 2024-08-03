using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.UIScope;
using DevLocker.Utils;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Modes;
using TetrisTower.TowerLevels.Playthroughs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.HomeScreen
{
	public class TowerLocationsSequenceController : MonoBehaviour, ILevelLoadedListener
	{
		// HACK: Because UnityEvents can't have two parameters - put it here.
		public PlaythroughTemplateBase TargetPlaythroughTemplate;

		public Transform LocationPreviewsRoot;
		public TowerLocationPreviewElement LocationPreviewElementPrefab;

		public WorldPlaythroughTemplate WorldPlaythroughTemplate;

		[SerializeField] private UIStepperNamed m_DifficultyStepper;
		[SerializeField] private UIStepperNamed m_SeedStepper;
		[SerializeField] private TMP_Text m_HighScoreValueText;

		public Button StartButton;

		public TowerDifficulty PickedDifficulty => (TowerDifficulty) (m_DifficultyStepper.SelectedIndex - 1);
		public int PickedSeed => m_SeedStepper.SelectedIndex switch {
			0 => 0,
			1 => 666,
			_ => throw new System.NotSupportedException($"Not supported seed selection {m_SeedStepper.SelectedIndex}."),
		};

		private GameContext m_GameContext;

		private TowerModesHighScoresDatabase HighScoresDatabase => GameManager.Instance.GetManager<TowerModesHighScoresDatabase>();

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameContext);

			LocationPreviewsRoot.DestroyChildren();

			var worldLevels = WorldPlaythroughTemplate.GetAllLevels().OfType<WorldMapLevelParamData>().ToList();
			WorldPlaythroughData storyInProgress = m_GameContext.StoryInProgress as WorldPlaythroughData;
			bool hasHiddenLocations = storyInProgress == null;

			foreach (LevelParamData levelParam in TargetPlaythroughTemplate.GetAllLevels()) {

				// Match from the world level to get the progress and image.
				WorldMapLevelParamData worldLevelParam = worldLevels.FirstOrDefault(wl => wl.BackgroundScene.ScenePath == levelParam.BackgroundScene.ScenePath);


				Sprite previewImage = worldLevelParam?.PreviewImage ?? null;

				bool isHidden = true;
				if (storyInProgress != null && worldLevelParam != null) {
					var locationState = storyInProgress.GetLocationState(worldLevelParam.LevelID);

					// First level should always be visible. World map may not have revealed it when saved.
					isHidden = locationState == WorldLocationState.Hidden;
					hasHiddenLocations |= isHidden;
				}

				var previewInstance = GameObject.Instantiate(LocationPreviewElementPrefab, LocationPreviewsRoot);
				previewInstance.SetPreview(previewImage, isHidden);
			}

			if (StartButton) {
				StartButton.interactable = !hasHiddenLocations;
			}

			m_DifficultyStepper.SelectedIndexChanged.AddListener(OnSelectedIndexChanged);
			m_SeedStepper.SelectedIndexChanged.AddListener(OnSelectedIndexChanged);
			RefreshPreview();
		}

		public void OnLevelUnloading()
		{
			m_DifficultyStepper.SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
			m_SeedStepper.SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
		}

		private void OnSelectedIndexChanged(int selectedIndex)
		{
			RefreshPreview();
		}

		private void RefreshPreview()
		{
			m_HighScoreValueText.text = HighScoresDatabase.GetHighScoreForMode(TargetPlaythroughTemplate, PickedDifficulty, PickedSeed).ToString();

		}
	}

}