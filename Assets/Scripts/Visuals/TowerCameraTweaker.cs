using System.Collections;
using System.Collections.Generic;
using TetrisTower.Platforms;
using UnityEngine;

namespace TetrisTower.Visuals
{
	/// <summary>
	/// Move the camera forward / back a bit depending if device is a phone, tablet or PC.
	/// Device orientation is also important - portrait is further back, while landscape brings the camera closer to the tower.
	/// </summary>
	public class TowerCameraTweaker : MonoBehaviour
	{
		public Vector2Int ZRangeOnMobile = new Vector2Int(50, 70);
		public AnimationCurve RatioToRangeOnMobile;

		private float m_InitialZ;
		private int m_ScreenPrevWidth = 0;
		private int m_ScreenPrevHeight = 0;

		private void Awake()
		{
			m_InitialZ = transform.localPosition.z;

			if (!PlatformsUtils.IsMobile && !PlatformsUtils.IsEditor) {
				enabled = false;
			}
		}

		void Update()
		{

			if (Screen.width != m_ScreenPrevWidth || Screen.height != m_ScreenPrevHeight) {

				Vector3 pos = transform.localPosition;

				if (!PlatformsUtils.IsMobileOrSimulator) {
					pos.z = m_InitialZ;
					transform.localPosition = pos;
					return;
				}

				float ratio = (float)Screen.width / Screen.height;

				float range = ZRangeOnMobile.y - ZRangeOnMobile.x;

				// Example values
				// | Expected device  | Ratio W/H  | Multiplier Value |
				// ====================================================
				// | Phone landscape  | 1.7777     | 1.0              |
				// | Tablet landscape | 1.3333     | 0.5              |
				// | Tablet portrait  | 0.75       | 0.5              |
				// | Phone portrait   | 0.5625     | 0.0              |
				pos.z = ZRangeOnMobile.x + Mathf.RoundToInt(range * RatioToRangeOnMobile.Evaluate(ratio));

				transform.localPosition = pos;
				m_ScreenPrevWidth = Screen.width;
				m_ScreenPrevHeight = Screen.height;
			}
		}

		private void OnValidate()
		{
			if (ZRangeOnMobile.y < ZRangeOnMobile.x) {
				ZRangeOnMobile.y = ZRangeOnMobile.x;
			}
		}
	}
}