using DevLocker.GFrame;
using DevLocker.GFrame.Pools;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TetrisTower.Logic;
using TetrisTower.Visuals;
using TMPro;
using UnityEngine;

namespace TetrisTower.TowerLevels.UI
{
	public class MatchSequenceScoreUIController : MonoBehaviour, ILevelLoadedListener
	{

		public RectTransformPrefabsPool EntryPool;

		public float EntryShowTweenDuration = 0.2f;
		public float EntryDisplayDuration = 2f;

		private class UIEntry
		{
			public RectTransform Transform;
			public CanvasGroup CanvasGroup;
			public Tween Tween;
			public Tween FadeOutTween;
		}

		private List<UIEntry> m_Entries = new List<UIEntry>();

		private ConeVisualsGrid m_VisualsGrid;

		public void OnLevelLoaded(LevelStateContextReferences contextReferences)
		{
			contextReferences.SetByType(out m_VisualsGrid);

			m_VisualsGrid.ScoreUpdated += OnUpdateScore;
			//m_VisualsGrid.ScoreFinished += OnFinishScore;

		}

		public void OnLevelUnloading()
		{
			m_VisualsGrid.ScoreUpdated -= OnUpdateScore;
			//m_VisualsGrid.ScoreFinished -= OnFinishScore;

			foreach(UIEntry entry in m_Entries) {
				KillEntry(entry);
			}
			m_Entries.Clear();

			m_VisualsGrid = null;
		}

		private void Awake()
		{
			EntryPool.Prefab.gameObject.SetActive(false);
		}

		private void OnValidate()
		{
			if (EntryPool == null) {
				Debug.LogError($"{name} - no entry pool specified.", this);
			} else if (EntryPool.Prefab && EntryPool.Prefab.GetComponentsInChildren<TextMeshProUGUI>().Length != 2) {
				Debug.LogError($"{name} entry pool prefab needs to have exactly 2 child texts!", this);
			}
		}

		public void OnUpdateScore(ScoreGrid scoreGrid)
		{
			PushUpEntries();

			RectTransform tr = EntryPool.Get(transform);
			UIEntry entry = new UIEntry() { Transform = tr, CanvasGroup = tr.GetComponent<CanvasGroup>() };
			m_Entries.Add(entry);

			// Ensure start values, pooled instance may come with random values.
			entry.Transform.anchoredPosition = Vector2.zero;
			entry.Transform.localScale = Vector3.one;
			if (entry.CanvasGroup) {
				entry.CanvasGroup.alpha = 1f;
			}

			entry.Tween = entry.Transform.DOScale(1f, EntryShowTweenDuration).SetEase(Ease.OutBack);

			TextMeshProUGUI[] texts = entry.Transform.GetComponentsInChildren<TextMeshProUGUI>();
			TextMeshProUGUI clearedBlocksText = texts[0];
			TextMeshProUGUI cascadeMultiplierText = texts[1];

			clearedBlocksText.text = "+" + scoreGrid.LastMatchBonus.ClearedBlocks.ToString();

			if (scoreGrid.LastMatchBonus.CascadeBonusMultiplier > 1) {
				cascadeMultiplierText.text = "x" + scoreGrid.LastMatchBonus.CascadeBonusMultiplier.ToString();
			} else {
				cascadeMultiplierText.text = "";
			}

			StartCoroutine(HideEntry(entry));
		}

		private void KillEntry(UIEntry entry)
		{
			entry.Tween?.Kill();
			entry.FadeOutTween?.Kill();
			EntryPool.Release(entry.Transform);
		}

		private void PushUpEntries()
		{
			float entryHeight = EntryPool.Prefab.sizeDelta.y;
			float scale = 1f;
			float posY = 0;

			for(int i = m_Entries.Count - 1; i >= 0; --i) {
				UIEntry entry = m_Entries[i];

				if (entry.Tween.IsActive()) {
					entry.Tween.Complete();
				}

				scale = Mathf.Max(scale - 0.2f, 0.1f);
				posY += entryHeight * scale;

				Sequence sequence = DOTween.Sequence();
				sequence.Append(entry.Transform.DOAnchorPosY(posY, EntryShowTweenDuration, true).SetEase(Ease.OutExpo));
				sequence.Join(entry.Transform.DOScale(scale, EntryShowTweenDuration).SetEase(Ease.OutExpo));

				entry.Tween = sequence;
			}
		}

		private IEnumerator HideEntry(UIEntry entry)
		{
			yield return new WaitForSeconds(EntryDisplayDuration);

			// Entry may get released if level is uninitialized.
			if (m_Entries.Contains(entry)) {
				if (entry.CanvasGroup) {
					entry.FadeOutTween = entry.CanvasGroup.DOFade(0f, EntryShowTweenDuration * 2f)
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