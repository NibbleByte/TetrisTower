using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class FairyMatchingController : MonoBehaviour
	{
		public float Speed = 4;
		public float RotationSpeed = 10;
		public Transform[] RestPoints { get; private set; }

		private Transform m_CurrentRestPoint = null;
		private Vector3 m_Heading;

		public void Init(IEnumerable<Transform> fairyRestPoints)
		{
			RestPoints = fairyRestPoints.ToArray();

			if (RestPoints.Length < 2) {
				Debug.LogError($"Fairy doesn't have enough rest points to patrol to! Disabling!", this);
				enabled = false;
				return;
			}

			m_CurrentRestPoint = RestPoints[UnityEngine.Random.Range(0, RestPoints.Length)];

			m_Heading = Vector3.one;
		}

		void Update()
		{
			if (Vector3.Distance(transform.position, m_CurrentRestPoint.position) < 0.2f) {
				var leftPoints = RestPoints.ToList();
				leftPoints.Remove(m_CurrentRestPoint);

				m_CurrentRestPoint = leftPoints[UnityEngine.Random.Range(0, leftPoints.Count)];
			}

			Vector3 dir = (m_CurrentRestPoint.position - transform.position).normalized;

			m_Heading = Vector3.RotateTowards(m_Heading, dir, RotationSpeed * Time.deltaTime, float.MaxValue);

			transform.position += m_Heading * Time.deltaTime * Speed;

			Debug.DrawLine(m_CurrentRestPoint.position, m_CurrentRestPoint.position + Vector3.up * 2, Color.blue);
		}
	}

}