/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;

namespace Kartographer
{
	public class Util
	{
		public const double		ONE_KMIN  = 60.0;
		public const double 	ONE_KHOUR = 60.0 * 60.0; 
		public const double 	ONE_KDAY  = 6.0 * 60.0 * 60.0; // Kerbin day's are 6 hours.
		public const double 	ONE_KYEAR = 426.0 * ONE_KDAY; // Kerbin years - 426 d 0 h 30 min
		public const double     ONE_KORBIT = 426.0 * ONE_KDAY + 32.0 * ONE_KMIN + 24.6; // Kerbin years - 426 d 0 h 32 min 24.6 s
		public const string 	VERSION = "0.1.0.3";
	}
}

