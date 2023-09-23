using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Draws gizmo displaying the camera frustum play area.
	/// </summary>
	public class TowerCameraFrustumAreaGizmo : MonoBehaviour
	{
		[Range(13, 13)]
		public int Columns = 13;
		public Transform Rotator;
		public Camera GameCamera;

#if UNITY_EDITOR

		private Vector3[] _frustumCornersCache = new Vector3[4];


		void OnDrawGizmos()
		{
			if (GameCamera == null || Rotator == null)
				return;

			GameCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), GameCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, _frustumCornersCache);

			Vector3 nearPlanePos = GameCamera.transform.TransformPoint(_frustumCornersCache[1]);

			Vector3 rotatorCenter = Rotator.position;
			rotatorCenter.y = nearPlanePos.y;

			float radius = Vector3.Distance(rotatorCenter, nearPlanePos);

			float coneSectorEulerAngle = 360f / Columns;

			for (int column = 0; column < Columns; ++column) {

				var leftVertex = GetFrustumPoint(coneSectorEulerAngle, column, rotatorCenter, radius);
				var rightVertex = GetFrustumPoint(coneSectorEulerAngle, column + 1, rotatorCenter, radius);

				Color prevColor = UnityEditor.Handles.color;
				UnityEditor.Handles.color = Color.red;

				UnityEditor.Handles.DrawLine(leftVertex, rightVertex);

				UnityEditor.Handles.color = prevColor;
			}
		}

		private Vector3 GetFrustumPoint(float coneSectorEulerAngle, int column, Vector3 rotatorCenter, float radius)
		{
			return rotatorCenter
				+ Quaternion.Euler(0f, -coneSectorEulerAngle * column + coneSectorEulerAngle / 2f, 0f)
				* GameCamera.transform.forward * radius
				;
		}
#endif

	}
}