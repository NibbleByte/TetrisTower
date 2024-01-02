using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	/// <summary>
	/// Handles camera effects like shake, etc.
	/// </summary>
	public class TowerCameraEffects : MonoBehaviour
	{
		private Vector3 m_InitialPos;
		private Dictionary<object, Vector2> m_ShakeRequests = new Dictionary<object, Vector2>();

		public void Shake(object source, Vector2 range) => m_ShakeRequests[source] = range;

		public bool ClearShake(object source) => m_ShakeRequests.Remove(source);


		void Awake()
		{
			m_InitialPos = transform.localPosition;
		}

		void Update()
		{
			if (m_ShakeRequests.Count == 0) {
				if (transform.localPosition != m_InitialPos) {
					transform.localPosition = m_InitialPos;
				}

			} else {
				Vector2 shakeRange = Vector2.zero;
				foreach(Vector2 shake in m_ShakeRequests.Values) {
					if (shake.sqrMagnitude > shakeRange.sqrMagnitude) {
						shakeRange = shake;
					}
				}

				transform.localPosition = m_InitialPos + new Vector3(Random.Range(shakeRange.x, shakeRange.y), Random.Range(shakeRange.x, shakeRange.y), 0f);
			}

		}
	}
}