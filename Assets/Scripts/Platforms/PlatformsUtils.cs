using UnityEngine;

namespace TetrisTower.Platforms
{
	/// <summary>
	/// Wraps up some platform calls and knowledge.
	/// </summary>
	public static class PlatformsUtils
	{

		/// <summary>
		/// Are we running on a mobile device, excluding the editor.
		/// </summary>
		public static bool IsMobile
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			=> true;
#else
			=> false;
#endif

		/// <summary>
		/// Are we running on a mobile device or in the editor mobile simulator.
		/// </summary>
		public static bool IsMobileOrSimulator => IsMobile || (Application.isEditor && Screen.dpi > InputMetrics.DefaultDPIForPC);  // This won't work on MacOS editor.

		public static bool IsEditor => Application.isEditor;
	}
}