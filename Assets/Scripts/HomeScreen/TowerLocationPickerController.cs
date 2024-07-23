using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using DevLocker.GFrame.Input.UIScope;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
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

		public Button StartButton;

		public LevelParamData PickedLevelParam => m_AllLevels[SelectedIndex];

		private GameContext m_GameContext;

		private WorldMapLevelParamData[] m_AllLevels;

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
			RefreshPreview();
		}

		public void OnLevelUnloading()
		{
			SelectedIndexChanged.RemoveListener(OnSelectedIndexChanged);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (Application.isPlaying) {
				// Reset every time - feels better.
				SelectedIndex = 0;
			}
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

			if (StartButton) {
				StartButton.interactable = !isHidden;
			}
		}
	}

}