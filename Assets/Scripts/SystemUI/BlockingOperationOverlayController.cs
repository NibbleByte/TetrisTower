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
		public class Scope : IDisposable
		{
			private BlockingOperationOverlayController m_Controller;
			private object m_Source;

			public Scope(BlockingOperationOverlayController controller, object source)
			{
				m_Controller = controller;
				m_Source = source;

				m_Controller.Block(m_Source);
			}

			public void Dispose()
			{
				m_Controller.Unblock(m_Source);
			}
		}

		public float DrawDelay = 1f;
		[SerializeField] private CanvasGroup Canvas;

		private readonly List<object> m_Blockers = new List<object>();

		void Awake()
		{
			gameObject.SetActive(false);
		}

		public Scope BlockScope(object source) => new Scope(this, source);

		public void Block(object source)
		{
			if (m_Blockers.Contains(source))
				return;

			if (m_Blockers.Count == 0) {
				gameObject.SetActive(true);
				Canvas.alpha = 0f;
				StartCoroutine(DelayedShow());
				GameManager.Instance.GameContext.GlobalInputContext.PushOrSetActionsMask(this, Array.Empty<InputAction>());
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

				GameManager.Instance.GameContext.GlobalInputContext.PopActionsMask(this);
			}

			return removed;
		}
	}
}