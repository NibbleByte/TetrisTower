using System;
using System.Collections;
using System.Collections.Generic;
using TetrisTower.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TetrisTower.SystemUI
{
	public class BlockingOperationOverlayController : MonoBehaviour
	{
		public float DrawDelay = 1f;
		[SerializeField] private CanvasGroup Canvas;

		private readonly List<object> m_Blockers = new List<object>();

		void Awake()
		{
			gameObject.SetActive(false);
		}

		public void Block(object source)
		{
			if (m_Blockers.Contains(source))
				return;

			if (m_Blockers.Count == 0) {
				gameObject.SetActive(true);
				Canvas.alpha = 0f;
				StartCoroutine(DelayedShow());
				GameManager.Instance.GameContext.InputContext.PushOrSetActionsMask(this, Array.Empty<InputAction>());
			}

			m_Blockers.Add(source);
		}

		// Show only if operation takes longer than the blink of an eye...
		private IEnumerator DelayedShow()
		{
			yield return new WaitForSeconds(DrawDelay);

			Canvas.alpha = 1f;
		}

		public bool Unblock(object source)
		{
			bool removed = m_Blockers.Remove(source);

			if (m_Blockers.Count == 0) {
				gameObject.SetActive(false);
				Canvas.alpha = 0f;

				GameManager.Instance.GameContext.InputContext.PopActionsMask(this);
			}

			return removed;
		}
	}
}