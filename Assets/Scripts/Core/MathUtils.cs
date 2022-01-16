using System;
using UnityEngine;

namespace TetrisTower.Core
{
	public static class MathUtils
	{
		/// <summary>
		/// Module that supports negative values.
		/// Thanks to PeterSvP via stackoverflow.com... :D
		/// </summary>
		public static double WrapValue(double value, double size)
		{
			return value - size * Math.Floor(value / size);
		}

		/// <summary>
		/// Module that supports negative values.
		/// Thanks to PeterSvP via stackoverflow.com... :D
		/// </summary>
		public static float WrapValue(float value, float size)
		{
			return value - size * (float)Math.Floor((float)value / size);
		}

		/// <summary>
		/// Module that supports negative values.
		/// Thanks to PeterSvP via stackoverflow.com... :D
		/// </summary>
		public static int WrapValue(int value, int size)
		{
			return value - size * (int)Math.Floor((float)value / size);
		}

		/// <summary>
		/// Re-maps a number from one range to another.
		/// </summary>
		/// <param name="value">Value from the first range.</param>
		/// <param name="from1">First range starting value.</param>
		/// <param name="to1">First range ending value.</param>
		/// <param name="from2">Second range starting value</param>
		/// <param name="to2">Second range ending value.</param>
		/// <returns></returns>
		public static float Remap(float value, float from1, float to1, float from2, float to2)
		{
			return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
		}
	}
}