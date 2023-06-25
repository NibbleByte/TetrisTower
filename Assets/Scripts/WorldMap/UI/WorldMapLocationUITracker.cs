using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TetrisTower.WorldMap.UI
{
	public class WorldMapLocationUITracker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public CanvasGroup CanvasGroup;
		public RectTransform AnimationRoot;
		public Image LocationImage;
		public GameObject CompletedImage;

		public AnimationCurve ZoomScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
		public AnimationCurve ZoomFadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		public float HoverScaleUp = 1.1f;
		private const float HoverScaleDuration = 0.2f;

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
			transform.localScale = Vector3.one * ZoomScaleCurve.Evaluate(zoom);

			CanvasGroup.alpha = ZoomFadeOutCurve.Evaluate(zoom);
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

			if (CompletedImage) {
				CompletedImage.SetActive(State == WorldLocationState.Completed);
			}
		}

		public Tween PlayRevealAnimation(float duration)
		{
			CanvasGroup.blocksRaycasts = false; // Avoids conflicts with hover animations.

			return AnimationRoot
				.DOScale(0.2f, duration)
				.From()
				.OnComplete(() => CanvasGroup.blocksRaycasts = true)
				;
		}

		public void OnClicked()
		{
			Clicked?.Invoke(m_LevelData.LevelID);
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			AnimationRoot.DOScale(HoverScaleUp, HoverScaleDuration);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			AnimationRoot.DOScale(1f, HoverScaleDuration);
		}
	}
}