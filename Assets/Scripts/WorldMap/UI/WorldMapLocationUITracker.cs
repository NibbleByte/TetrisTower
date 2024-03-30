using DG.Tweening;
using System;
using TetrisTower.TowerLevels.Playthroughs;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TetrisTower.WorldMap.UI
{
	public class WorldMapLocationUITracker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public CanvasGroup CanvasGroup;
		public RectTransform AnimationRoot;
		public Image LocationImage;
		public GameObject[] StarsCostElements;
		public Image[] EarnedStarImages;

		public GameObject CompletedImage;

		public Sprite EarnedStarSprite;
		public Sprite MissingStarSprite;

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

			foreach(GameObject element in StarsCostElements) {
				var elementText = element.GetComponentInChildren<TMP_Text>(true);
				if (elementText) {
					elementText.text = levelData.StarsCost.ToString();
				}
			}
		}

		public void SetZoomLevel(float zoom)
		{
			transform.localScale = Vector3.one * ZoomScaleCurve.Evaluate(zoom);

			CanvasGroup.alpha = ZoomFadeOutCurve.Evaluate(zoom);
		}

		public void SetState(WorldLocationState state, int starsEarned)
		{
			State = state;

			switch (State) {
				case WorldLocationState.Hidden:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Reached:
					gameObject.SetActive(false);
					break;

				case WorldLocationState.Revealed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				case WorldLocationState.Unlocked:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				case WorldLocationState.Completed:
					// TODO: Colorize differently
					gameObject.SetActive(true);
					break;

				default: throw new System.NotSupportedException(State.ToString());
			}

			for (int i = 0; i < EarnedStarImages.Length; i++) {
				Image earnedStarImage = EarnedStarImages[i];
				earnedStarImage.gameObject.SetActive(State == WorldLocationState.Completed);

				earnedStarImage.sprite = i + 1 <= starsEarned ? EarnedStarSprite : MissingStarSprite;
			}

			foreach(GameObject element in StarsCostElements) {
				element.SetActive(State == WorldLocationState.Revealed);
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

		public Tween PlayUnlockAnimation(float duration)
		{
			AnimationRoot.DOComplete(true);

			return AnimationRoot
				.DOScale(AnimationRoot.localScale.x + 0.2f, duration)
				.SetLoops(2, LoopType.Yoyo)
				;
		}

		public Tween PlayRejectShake(float duration)
		{
			AnimationRoot.DOComplete(true);

			return AnimationRoot.DOShakeAnchorPos(duration, new Vector2(10f, 2f), 20);
		}

		public void OnClicked()
		{
			Clicked?.Invoke(m_LevelData.LevelID);
		}

		void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
		{
			AnimationRoot.DOComplete(true);

			AnimationRoot.DOScale(HoverScaleUp, HoverScaleDuration);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			AnimationRoot.DOComplete(true);

			AnimationRoot.DOScale(1f, HoverScaleDuration);
		}
	}
}