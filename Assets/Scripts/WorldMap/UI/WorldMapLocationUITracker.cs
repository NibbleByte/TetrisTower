using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.WorldMap.UI
{
	public class WorldMapLocationUITracker : MonoBehaviour
	{
		public CanvasGroup CanvasGroup;
		public Image LocationImage;

		public AnimationCurve ZoomFadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		public event Action<string> Clicked;

		private WorldMapLevelParamData m_LevelData;


		public WorldLocationState State { get; private set; }

		public void Setup(WorldMapLevelParamData levelData)
		{
			m_LevelData = levelData;

			LocationImage.sprite = m_LevelData.PreviewImage;
		}

		public void SetZoomLevel(float zoom)
		{
			if (CanvasGroup) {
				CanvasGroup.alpha = ZoomFadeOutCurve.Evaluate(zoom);
			}
		}

		public void SetState(WorldLocationState state)
		{
			State = state;

			switch (State) {
				case WorldLocationState.Hidden:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Unlocked:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Revealed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				case WorldLocationState.Completed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				default: throw new System.NotSupportedException(State.ToString());
			}
		}

		public void OnClicked()
		{
			Clicked?.Invoke(m_LevelData.LevelID);
		}
	}
}