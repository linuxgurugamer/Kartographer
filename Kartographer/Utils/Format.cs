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
			if (value > 1e12d || (value < 1e-2d && value > 0.0d)) {
				return value.ToString ("e6") + unit;
			} else if (value > 1e7d) {
				unit = " M";
				value /= 1e6d;
			} else if (value > 1e4d) {
				unit = " k";
				value /= 1e3d;
			}
			string result = value.ToString ("N2") + unit;
			return result;
		}

		/// <summary>
		/// Gets the time string.
		/// </summary>
		/// <returns>The time string.</returns>
		/// <param name="value">Value.</param>
		internal static string GetUTTimeString (double value)
		{
			return GetTimeString (value + ONE_KYEAR + ONE_KDAY);
		}

		/// <summary>
		/// Gets the time string.
		/// </summary>
		/// <returns>The time string.</returns>
		/// <param name="value">Value.</param>
		internal static string GetTimeString (double value)
		{
			string result = "";
			if (value < 0.0) {
				result += "-";
				value = Math.Abs (value);
			}
			if (value > ONE_KYEAR) {
				int years = (int)value / (int)ONE_KYEAR;
				value -= (years * ONE_KYEAR);
				result += years + " y,";
			}
			if (value > ONE_KDAY) {
				int days = (int)value / ((int)ONE_KDAY);
				value -= days * ONE_KDAY;
				result += days + " d,";
			}
			if (value > ONE_KHOUR) {
				int hours = (int)value / ((int)ONE_KHOUR);
				value -= hours * ONE_KHOUR;
				result += hours + " h,";
			}

			if (value > ONE_KMIN) {
				int mins = (int)value / 60;
				value -= mins * 60;
				result += mins + " m,";
			}
			result += value.ToString ("N2") + " s";
			return result;
		}
	}
}
