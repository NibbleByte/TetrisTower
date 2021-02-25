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
	}
}