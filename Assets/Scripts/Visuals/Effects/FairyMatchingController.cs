using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TetrisTower.Visuals.Effects
{
	public class FairyMatchingController : MonoBehaviour
	{
		public float IdleSpeed = 12;
		public float RotationSpeed = 10;
		public float SpeedCatchupRotation = 16;

		public Transform[] RestPoints { get; private set; }

		private Transform m_CurrentRestPoint = null;
		private Vector3 m_Heading;
		private float m_Speed;

		private Vector3 m_TowerBaseCenter;
		private float m_TowerRadius;

		private float m_ChaseRestMinDistance;

		public void Init(IEnumerable<Transform> fairyRestPoints, Vector3 towerBaseCenter, float towerRadius)
		{
			RestPoints = fairyRestPoints.ToArray();

			if (RestPoints.Length < 2) {
				Debug.LogError($"Fairy doesn't have enough rest points to patrol to! Disabling!", this);
				enabled = false;
				return;
			}

			m_CurrentRestPoint = RestPoints[UnityEngine.Random.Range(0, RestPoints.Length)];

			m_Heading = Vector3.one;
			m_Speed = IdleSpeed;

			m_TowerBaseCenter = towerBaseCenter;
			m_TowerRadius = towerRadius * 1.2f;	// Inflate a bit


			m_ChaseRestMinDistance = 0;

			foreach(Transform p1 in RestPoints) {
				foreach(Transform p2 in RestPoints) {
					float dist = Vector3.Distance(p1.position, p2.position) * 1.2f;
					if (dist > m_ChaseRestMinDistance) {
						m_ChaseRestMinDistance = dist;
					}
				}
			}
		}

		void Update()
		{
			if (Vector3.Distance(transform.position, m_CurrentRestPoint.position) < 0.2f) {
				var leftPoints = RestPoints.ToList();
				leftPoints.Remove(m_CurrentRestPoint);

				m_CurrentRestPoint = leftPoints[UnityEngine.Random.Range(0, leftPoints.Count)];
			}

			Vector3 dist = m_CurrentRestPoint.position - transform.position;
			Vector3 dir = dist.normalized;

			float targetSpeed = IdleSpeed;
			float rotateSpeed = RotationSpeed;

			// Too far away, move around the tower to get close to rest points.
			if (dist.magnitude > m_ChaseRestMinDistance) {
				Vector3 closePoint = transform.position + dist * 0.2f;

				Vector3 center = m_TowerBaseCenter;
				center.y = closePoint.y;

				Vector3 targetWaypoint = m_TowerRadius * (closePoint - center).normalized + center;

				dist = targetWaypoint - transform.position;
				dir = dist.normalized;
				targetSpeed = SpeedCatchupRotation;

				// Moving away too much can cause sudden rotate to 180, which doesn't look good if limited.
				rotateSpeed = RotationSpeed * 4;

				Debug.DrawLine(targetWaypoint, targetWaypoint + Vector3.up * 2, Color.red);
			}

			m_Heading = Vector3.RotateTowards(m_Heading, dir, rotateSpeed * Time.deltaTime, float.MaxValue);

			m_Speed = Mathf.Lerp(m_Speed, targetSpeed, Time.deltaTime * 2f);

			transform.position += m_Heading * Time.deltaTime * m_Speed;

			Debug.DrawLine(m_CurrentRestPoint.position, m_CurrentRestPoint.position + Vector3.up * 2, Color.blue);
		}
	}

}