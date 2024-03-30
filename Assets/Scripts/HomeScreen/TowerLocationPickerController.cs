using DevLocker.GFrame;
using DevLocker.GFrame.Input;
using DevLocker.GFrame.Input.Contexts;
using System.Linq;
using TetrisTower.Core;
using TetrisTower.Game;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.HomeScreen
{
	public class TowerLocationPickerController : MonoBehaviour, ILevelLoadedListener
	{
		// HACK: Because UnityEvents can't have two parameters - put it here.
		public PlaythroughTemplateBase TargetPlaythroughTemplate;

		public Image LocationPreview;
		public GameObject HiddenIndicator;

		public WorldPlaythroughTemplate WorldPlaythroughTemplate;

		public Button StartButton;

		public LevelParamData PickedLevelParam => m_AllLevels[m_DisplayedIndex];

		private GameContext m_GameContext;

		private WorldMapLevelParamData[] m_AllLevels;
		private int m_DisplayedIndex = 0;

		public void OnLevelLoaded(PlayerStatesContext context)
		{
			context.SetByType(out m_GameContext);


			if (m_GameContext.StoryInProgress != null) {
				m_AllLevels = ((WorldPlaythroughData)m_GameContext.StoryInProgress)?.GetAllLevels().OfType<WorldMapLevelParamData>().ToArray();
			} else {
				m_AllLevels = WorldPlaythroughTemplate.GetAllLevels().OfType<WorldMapLevelParamData>().ToArray();
			}

			m_DisplayedIndex = 0;
			RefreshPreview();
		}

		public void OnLevelUnloading()
		{

		}

		void OnEnable()
		{
			// Reset every time - feels better.
			m_DisplayedIndex = 0;
			RefreshPreview();
		}

		public void NextLocation()
		{
			m_DisplayedIndex = MathUtils.WrapValue(m_DisplayedIndex + 1, m_AllLevels.Length);
			RefreshPreview();
		}

		public void PrevLocation()
		{
			m_DisplayedIndex = MathUtils.WrapValue(m_DisplayedIndex - 1, m_AllLevels.Length);
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

			LocationPreview.sprite = m_AllLevels[m_DisplayedIndex].PreviewImage;

			bool isHidden;

			if (m_GameContext.StoryInProgress != null) {
				var playthroughData = (WorldPlaythroughData)m_GameContext.StoryInProgress;
				var locationState = playthroughData.GetLocationState(m_AllLevels[m_DisplayedIndex].LevelID);
				// First level should always be visible. World map may not have revealed it when saved.
				isHidden = locationState == WorldLocationState.Hidden && m_DisplayedIndex != 0;

			} else {
				isHidden = m_DisplayedIndex != 0;
			}

			LocationPreview.color = isHidden ? grayColor : Color.white;
			HiddenIndicator.SetActive(isHidden);

			if (StartButton) {
				StartButton.interactable = !isHidden;

				PlayerContextUIRootObject.GlobalPlayerContext.SetSelectedGameObject(StartButton.gameObject);
			}
		}
	}

}