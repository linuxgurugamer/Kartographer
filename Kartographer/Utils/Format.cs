/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;

namespace Kartographer
{
	internal class Format
	{
		internal const double ONE_KMIN = 60.0;
		internal const double ONE_KHOUR = 60.0 * 60.0;
		internal const double ONE_KDAY = 6.0 * 60.0 * 60.0; // Kerbin day's are 6 hours.
		internal const double ONE_KYEAR = 426.0 * ONE_KDAY; // Kerbin years - 426 d 0 h 30 min
		internal const double ONE_KORBIT = 426.0 * ONE_KDAY + 32.0 * ONE_KMIN + 24.6; // Kerbin years - 426 d 0 h 32 min 24.6 s
		internal const string VERSION = "0.1.0.3";

		/// <summary>
		/// Takes a number and formats it for display. Uses standard metric prefixes or scientific notation.
		/// </summary>
		/// <returns>The number string.</returns>
		/// <param name="value">Value.</param>
		internal static string GetNumberString (double value)
		{
			string unit = " ";
			if (value > 1e18d || (value < 1e-2d && value > 0.0d)) {
				return value.ToString ("e6") + unit;
			} else if (value > 1e15d) {
				unit = " P";
			} else if (value > 1e12d) {
				unit = " T";
			} else if (value > 1e9d) {
				unit = " G";
			} else if (value > 1e6d) {
				unit = " M";
				value /= 1e6d;
			} else if (value > 1e3d) {
				unit = " k";
				value /= 1e3d;
			}
			string result = value.ToString ("N3") + unit;
			return result;
		}
	}
}
