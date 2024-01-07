using DevLocker.GFrame.Pools;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TetrisTower.SystemUI
{
	public class ToastNotificationsController : MonoBehaviour
	{
		private class ElementEntry
		{
			public string Message;
			public ToastNotificationElement Element;
			public RectTransform Transform;
			public CanvasGroup CanvasGroup;
			public Tween ShowTween;
			public Tween HideTween;
		}

		public RectTransformPrefabsPool EntryPool;
		private float ElementHeight => EntryPool.Prefab.sizeDelta.y;

		public float EntriesPadding = 0f;
		public float EntryShowUpDuration = 0.2f;
		public float EntryDisplayDuration = 2f;

		private List<ElementEntry> m_Entries = new List<ElementEntry>();

		void Awake()
		{
			EntryPool.Prefab.gameObject.SetActive(false);
		}

		void OnDestroy()
		{
			foreach (ElementEntry entry in m_Entries) {
				KillEntry(entry);
			}
			m_Entries.Clear();
		}

		public void ShowNotification(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return;

			if (message == m_Entries.LastOrDefault()?.Message)
				return;

			RectTransform elementTransform = EntryPool.Get(transform);

			var entry = new ElementEntry() {
				Message = message,
				Element = elementTransform.GetComponent<ToastNotificationElement>(),
				Transform = elementTransform,
				CanvasGroup = elementTransform.GetComponent<CanvasGroup>(),
			};

			entry.Element.SetMessage(message);
			entry.Element.CloseRequested += OnCloseRequested;

			// Ensure start values, pooled instance may come with random values.
			entry.Transform.anchoredPosition = new Vector2(0f, - ElementHeight - EntriesPadding);
			entry.Transform.localScale = Vector3.one;
			if (entry.CanvasGroup) {
				entry.CanvasGroup.alpha = 1f;
			}

			m_Entries.Add(entry);

			RefreshEntryPositions();

			// Will kill the coroutine when destroyed or deactivated.
			entry.Element.StartCoroutine(HideEntry(entry));
		}

		private void KillEntry(ElementEntry entry)
		{
			entry.ShowTween?.Kill();
			entry.HideTween?.Kill();
			entry.Element.CloseRequested -= OnCloseRequested;
			EntryPool.Release(entry.Transform);
		}

		private void RefreshEntryPositions()
		{
			for (int i = 0; i < m_Entries.Count; ++i) {
				ElementEntry entry = m_Entries[i];

				if (entry.ShowTween.IsActive()) {
					entry.ShowTween.Kill();
				}

				float posY = (m_Entries.Count - i - 1) * (ElementHeight + EntriesPadding);

				Sequence sequence = DOTween.Sequence();
				sequence.Append(entry.Transform.DOAnchorPosY(posY, EntryShowUpDuration, true).SetEase(Ease.OutExpo));

				entry.ShowTween = sequence;
			}
		}

		private void OnCloseRequested(ToastNotificationElement element)
		{
			var entry = m_Entries.First(e => e.Element == element);
			KillEntry(entry);
			m_Entries.Remove(entry);

			RefreshEntryPositions();
		}

		private IEnumerator HideEntry(ElementEntry entry)
		{
			yield return new WaitForSeconds(EntryDisplayDuration);

			// Entry may get released if level is uninitialized.
			if (m_Entries.Contains(entry)) {
				if (entry.CanvasGroup) {
					entry.HideTween = entry.CanvasGroup.DOFade(0f, EntryShowUpDuration * 2f)
						.SetEase(Ease.InExpo)
						.OnComplete(() => { KillEntry(entry); m_Entries.Remove(entry); });
				} else {
					KillEntry(entry);
					m_Entries.Remove(entry);
				}
			}
		}
	}
}