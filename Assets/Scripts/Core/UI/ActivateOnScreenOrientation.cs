using System;
using System.Collections.Generic;
using UnityEngine;

namespace TetrisTower.Core.UI
{
	[ExecuteInEditMode]
	public class ActivateOnScreenOrientation : MonoBehaviour
	{
		public float ScreenLandscapeRatio = 1;

		public GameObject PortraitObject;
		public GameObject LandscapeObject;

		private void Update()
		{
			bool isLandscape = ((float)Screen.width / Screen.height) >= ScreenLandscapeRatio;

			if (PortraitObject) PortraitObject.SetActive(!isLandscape);
			if (LandscapeObject) LandscapeObject.SetActive(isLandscape);
		}
	}
}