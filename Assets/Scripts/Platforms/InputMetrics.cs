using UnityEngine;

namespace TetrisTower.Platforms
{
	public static class InputMetrics
	{
		public const float DefaultDPIForPC = 96f;

		/// <summary>
		/// Input precision compared to standard PC display (i.e. 96 DPI).
		/// Multiply this to scale your values with the DPI.
		/// </summary>
		public static float InputPrecision => Screen.dpi / DefaultDPIForPC;
	}
}