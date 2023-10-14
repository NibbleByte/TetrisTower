using DevLocker.GFrame.Timing;
using System;
using System.Collections;
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
		public float IdleRestPointTimeout = 2f;

		public float ChaseSpeed = 12;
		public float ChaseRotation = 16;
		public float ChaseEndDelay = 0.2f;

		public Transform[] RestPoints { get; private set; }

		private Transform m_CurrentRestPoint = null;
		private float m_CurrentRestPointTime = 0f;
		private Vector3 m_Heading;
		private float m_Speed;

		private Vector3 m_TowerBaseCenter;
		private float m_TowerRadius;

		private float m_ChaseRestMinDistance;

		private Vector3? m_ChasePos;
		private Vector3? m_ChasePosWaypoint;
		private float m_ChaseEndDuration = 0;

		private WiseTiming m_Timing;

		public void Init(IEnumerable<Transform> fairyRestPoints, Vector3 towerBaseCenter, float towerRadius, WiseTiming timing)
		{
			RestPoints = fairyRestPoints.ToArray();
			m_Timing = timing;

			if (RestPoints.Length < 2) {
				Debug.LogError($"Fairy doesn't have enough rest points to patrol to! Disabling!", this);
				enabled = false;
				return;
			}

			m_CurrentRestPoint = RestPoints[UnityEngine.Random.Range(0, RestPoints.Length)];

			m_Heading = Vector3.one;
			m_Speed = IdleSpeed;

			m_TowerBaseCenter = towerBaseCenter;
			m_TowerRadius = towerRadius;


			m_ChaseRestMinDistance = 0;

			foreach(Transform p1 in RestPoints) {
				foreach(Transform p2 in RestPoints) {
					float dist = Vector3.Distance(p1.position, p2.position) * 1.2f;
					if (dist > m_ChaseRestMinDistance) {
						m_ChaseRestMinDistance = dist;
					}
				}
			}

			m_Timing.PreUpdate += LevelUpdate;
		}

		public IEnumerator ChaseTo(Vector3 chasePos)
		{
			m_ChasePos = chasePos;
			m_ChaseEndDuration = 0f;

			Vector3 dist = m_ChasePos.Value - transform.position;
			Vector3 bumpPoint = transform.position + dist * 0.5f;

			Vector3 center = m_TowerBaseCenter;
			center.y = bumpPoint.y;

			m_ChasePosWaypoint = 1.0f * m_TowerRadius * (bumpPoint - center).normalized + center;

			while (m_ChasePos.HasValue) {
				yield return null;
			}
		}

		private void IdleUpdate()
		{
			if (Vector3.Distance(transform.position, m_CurrentRestPoint.position) < 0.2f || m_CurrentRestPointTime > IdleRestPointTimeout) {
				var leftPoints = RestPoints.ToList();
				leftPoints.Remove(m_CurrentRestPoint);

				m_CurrentRestPoint = leftPoints[UnityEngine.Random.Range(0, leftPoints.Count)];
				m_CurrentRestPointTime = 0f;

			} else {
				m_CurrentRestPointTime += WiseTiming.DeltaTime;
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

				Vector3 targetWaypoint = 1.2f * m_TowerRadius * (closePoint - center).normalized + center;

				dist = targetWaypoint - transform.position;
				dir = dist.normalized;
				targetSpeed = SpeedCatchupRotation;

				// Moving away too much can cause sudden rotate to 180, which doesn't look good if limited.
				rotateSpeed = RotationSpeed * 4;

				Debug.DrawLine(targetWaypoint, targetWaypoint + Vector3.up * 2, Color.red);

				m_CurrentRestPointTime = 0;
			}


			m_Heading = Vector3.RotateTowards(m_Heading, dir, rotateSpeed * WiseTiming.DeltaTime, float.MaxValue);

			m_Speed = Mathf.Lerp(m_Speed, targetSpeed, WiseTiming.DeltaTime * 2f);

			transform.position += m_Heading * WiseTiming.DeltaTime * m_Speed;

			Debug.DrawLine(m_CurrentRestPoint.position, m_CurrentRestPoint.position + Vector3.up * 2, Color.blue);
		}

		private void ChaseUpdate()
		{
			Vector3 targetPos = (m_ChasePosWaypoint.HasValue) ? m_ChasePosWaypoint.Value : m_ChasePos.Value;

			Debug.DrawLine(targetPos, targetPos + Vector3.up * 2, Color.red);

			m_Speed = Mathf.Lerp(m_Speed, ChaseSpeed, WiseTiming.DeltaTime * 2f);

			transform.position = Vector3.MoveTowards(transform.position, targetPos, m_Speed * WiseTiming.DeltaTime);

			if (m_ChasePosWaypoint.HasValue && Vector3.Distance(transform.position, m_ChasePosWaypoint.Value) < 0.2f) {
				m_ChasePosWaypoint = null;
				return;
			}

			if (Vector3.Distance(transform.position, m_ChasePos.Value) < 0.2f) {
				m_ChaseEndDuration += WiseTiming.DeltaTime;

				if (m_ChaseEndDuration >= ChaseEndDelay) {
					m_Speed = ChaseSpeed * 0.3f;
					m_ChasePos = null;
					m_ChasePosWaypoint = null;
					m_ChaseEndDuration = 0f;

					// Head away from the tower.
					Vector3 center = m_TowerBaseCenter;
					center.y = transform.position.y;
					m_Heading = (transform.position - center).normalized;

					m_CurrentRestPointTime = 0f;
				}
				return;
			}
		}

		private void LevelUpdate()
		{
			if (m_ChasePos.HasValue) {
				ChaseUpdate();
			} else {
				IdleUpdate();
			}
		}

		// Not needed anymore.
		//void LateUpdate()
		//{
		//	transform.LookAt(Camera.main.transform);
		//}
	}

}