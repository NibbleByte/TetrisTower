using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.UIScope;
using System.Linq;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Modes;
using TetrisTower.TowerLevels.Playthroughs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.HomeScreen
{
	public class TowerLocationPickerController : UIStepperNamed, ILevelLoadedListener
	{
		// HACK: Because UnityEvents can't have two parameters - put it here.
		public PlaythroughTemplateBase TargetPlaythroughTemplate;

		public Image LocationPreview;
		public GameObject HiddenIndicator;

		public WorldPlaythroughTemplate WorldPlaythroughTemplate;

		[SerializeField] private UIStepperNamed m_DifficultyStepper;
		[SerializeField] private UIStepperNamed m_SeedStepper;
		[SerializeField] private TMP_Text m_HighScoreValueText;

		public Button StartButton;

		public WorldMapLevelParamData PickedLevelParam => m_AllLevels[SelectedIndex];

		public TowerDifficulty PickedDifficulty => (TowerDifficulty) (m_DifficultyStepper.SelectedIndex - 1);
		public int PickedSeed => m_SeedStepper.SelectedIndex switch {
			0 => 0,
			1 => 666,
			_ => throw new System.NotSupportedException($"Not supported seed selection {m_SeedStepper.SelectedIndex}."),
		};

		private GameContext m_GameContext;

		private WorldMapLevelParamData[] m_AllLevels;

		private TowerModesHighScoresDatabase HighScoresDatabase => GameManager.Instance.GetManager<TowerModesHighScoresDatabase>();

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameContext);


			if (m_GameContext.StoryInProgress != null) {
				m_AllLevels = ((WorldPlaythroughData)m_GameContext.StoryInProgress)?.GetAllLevels().OfType<WorldMapLevelParamData>().ToArray();
			} else {
				m_AllLevels = WorldPlaythroughTemplate.GetAllLevels().OfType<WorldMapLevelParamData>().ToArray();
			}

			Options = m_AllLevels.Select(lp => lp.PreviewImage.name).ToArray();
			SelectedIndex = 0;

			SelectedIndexChanged.AddListener(OnSelectedIndexChanged);
			m_DifficultyStepper.SelectedIndexChanged.AddListener(OnSelectedIndexChanged);
			m_SeedStepper.SelectedIndexChanged.AddListener(OnSelectedIndexChanged);
			RefreshPreview();
		}

		public void OnLevelUnloading()
		{
			SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
			m_DifficultyStepper.SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
			m_SeedStepper.SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
		}

		private void OnSelectedIndexChanged(int selectedIndex)
		{
			RefreshPreview();
		}

		private void RefreshPreview()
		{
			Color grayColor = new Color(0.3f, 0.3f, 0.3f, 1f);

			if (m_AllLevels.Length == 0) {
				LocationPreview.color = grayColor;
				HiddenIndicator.SetActive(true);

				if (StartButton) {
					StartButton.interactable = false;
				}

				return;
			}

			LocationPreview.sprite = m_AllLevels[SelectedIndex].PreviewImage;

			bool isHidden;

			if (m_GameContext.StoryInProgress != null) {
				var playthroughData = (WorldPlaythroughData)m_GameContext.StoryInProgress;
				var locationState = playthroughData.GetLocationState(m_AllLevels[SelectedIndex].LevelID);
				// First level should always be visible. World map may not have revealed it when saved.
				isHidden = locationState == WorldLocationState.Hidden && SelectedIndex != 0;

			} else {
				isHidden = SelectedIndex != 0;
			}

			LocationPreview.color = isHidden ? grayColor : Color.white;
			HiddenIndicator.SetActive(isHidden);

			m_HighScoreValueText.text = HighScoresDatabase.GetHighScoreForMode(TargetPlaythroughTemplate, PickedLevelParam.LevelID, PickedDifficulty, PickedSeed).ToString();

			if (StartButton) {
				StartButton.interactable = !isHidden;
			}
		}
	}

}