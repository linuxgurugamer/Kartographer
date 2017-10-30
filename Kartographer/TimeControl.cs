/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;

namespace Kartographer
{
	public class TimeControl
	{
		public double UT { get { return _UT; } }

		const int MAX_TIME_GRAN = 2;

		double _UT = 0.0d;
		int _timeGranularity = 1;
		int _menuSelection = 0;

		public TimeControl ()
		{
		}

		/// <summary>
		/// Gets Ascending Node's true anomaly given a current and target orbit.
		/// </summary>
		/// <returns>The AN true anomaly.</returns>
		/// <param name="obt">Obt.</param>
		/// <param name="tgtObt">Tgt obt.</param>
		double GetANTrueAnomaly (Orbit obt, Orbit tgtObt)
		{
			double anTa = 0.0d;
			if (obt.referenceBody == tgtObt.referenceBody) {
				// There are easier ways, but go ahead and work out the AN/DN from scratch.
				// It is a good excuse to learn some more orbital mechanics.
				double iRad = obt.inclination * Math.PI / 180.0d;
				double LANRad = obt.LAN * Math.PI / 180.0d;

				double a1 = Math.Sin (iRad) * Math.Cos (LANRad);
				double a2 = Math.Sin (iRad) * Math.Sin (LANRad);
				double a3 = Math.Cos (iRad);
				Vector3d a = new Vector3d (a1, a2, a3);

				double tgtiRad = tgtObt.inclination * Math.PI / 180.0d;
				double tgtLANRad = tgtObt.LAN * Math.PI / 180.0d;

				double b1 = Math.Sin (tgtiRad) * Math.Cos (tgtLANRad);
				double b2 = Math.Sin (tgtiRad) * Math.Sin (tgtLANRad);
				double b3 = Math.Cos (tgtiRad);
				Vector3d b = new Vector3d (b1, b2, b3);

				Vector3d c = Vector3d.Cross (a, b);

				// Determine celestial longitude of the cross over.
				double lon = Math.Atan2 (c.y, c.x);
				while (lon < 0.0d)
					lon += 2 * Math.PI;

				// Angle of crossover.
				double theta = Math.Acos (Vector3d.Dot (a, b)) * 180.0d / Math.PI;

				// Convert true longitude to true anomaly.
				double nodeTaRaw = lon - (obt.argumentOfPeriapsis + obt.LAN) * Math.PI / 180.0 + (c.x < 0 ? Math.PI / 2.0d : Math.PI * 3.0d / 2.0d);

				// Figure out which node we found and setup the other one.
				if (theta > 0.0d) {
					anTa = nodeTaRaw;
				} else {
					anTa = nodeTaRaw + Math.PI;
				}
			}
			return anTa;
		}

		/// <summary>
		/// Draw the time GUI and returns any change in UT.
		/// </summary>
		/// <returns>The UT, either the current ut or the updated value.</returns>
		/// <param name="ut">Current UT.</param>
		/// <param name="vessel">Vessel.</param>
		public double TimeGUI (double ut, Vessel vessel = null)
		{
			_UT = ut;
			if (vessel == null) {
				GUILayout.Label ("Time Controls");
				DrawTimeControls ();
			} else {
				_menuSelection = GUILayout.SelectionGrid (_menuSelection,
					new string [] { "Time Controls", "Orbit Events" }, 3);
				if (_menuSelection == 0) {
					DrawTimeControls ();
				} else {
					DrawEventControls (vessel);
				}
			}
			return _UT;
		}

		/// <summary>
		/// Draws the event controls.
		/// </summary>
		/// <param name="vessel">Vessel.</param>
		void DrawEventControls (Vessel vessel)
		{
			Orbit obt = vessel.orbit;
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Ap") && obt.timeToAp > 0.0d) {
				_UT = Planetarium.GetUniversalTime () + obt.timeToAp;
			}
			if (GUILayout.Button ("Pe") && obt.timeToPe > 0.0d) {
				_UT = Planetarium.GetUniversalTime () + obt.timeToPe;
			}

			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			double atmos = obt.referenceBody.atmosphereDepth;
			if (atmos > 0.0d && obt.PeA < atmos && (obt.ApA > atmos || obt.ApA < 0)) {
				double atmosR = obt.referenceBody.Radius + atmos;
				double atmosTa = obt.TrueAnomalyAtRadius (atmosR);

				if (GUILayout.Button ("Atmos Exit")) {
					double atmosUT = obt.GetUTforTrueAnomaly (atmosTa, Planetarium.GetUniversalTime ());
					while (atmosUT < Planetarium.GetUniversalTime ())
						atmosUT += obt.period;
					_UT = atmosUT;
				}
				if (GUILayout.Button ("Atmos Enter")) {
					double atmosUT = obt.GetUTforTrueAnomaly (2 * Math.PI - atmosTa, Planetarium.GetUniversalTime ());
					while (atmosUT < Planetarium.GetUniversalTime ())
						atmosUT += obt.period;
					_UT = atmosUT;
				}
			} else if (vessel.orbit.patchEndTransition == Orbit.PatchTransitionType.FINAL) {
				GUILayout.Label ("No SOI changes.");
			}
			Orbit trans = vessel.orbit;
			while (trans.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
					!vessel.Landed && trans.activePatch && trans.nextPatch != null && trans.nextPatch.activePatch) {
				if (GUILayout.Button ("SOI:" + trans.nextPatch.referenceBody.RevealName ())) {
					// Warp to SOI transition.
					_UT = trans.EndUT - 10.0d;
				}
				trans = trans.nextPatch;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();

			if (vessel.targetObject != null) {
				Orbit tgtObt = vessel.targetObject.GetOrbit ();
				if (obt.referenceBody == tgtObt.referenceBody) {
					double anTa = GetANTrueAnomaly (obt, tgtObt);
					double dnTa = anTa + Math.PI;

					if (GUILayout.Button ("AN")) {
						double ut = obt.GetUTforTrueAnomaly (anTa, Planetarium.GetUniversalTime ());
						while (ut < Planetarium.GetUniversalTime ())
							ut += obt.period;
						_UT = ut;
					}
					if (GUILayout.Button ("DN")) {
						double ut = obt.GetUTforTrueAnomaly (dnTa, Planetarium.GetUniversalTime ());
						while (ut < Planetarium.GetUniversalTime ())
							ut += obt.period;
						_UT = ut;
					}
				} else {
					GUILayout.Label ("Target orbits a different body.");
				}
			} else {
				GUILayout.Label ("No Target.");
			}
			GUILayout.EndHorizontal ();

		}

		/// <summary>
		/// Draws the time controls.
		/// </summary>
		void DrawTimeControls ()
		{
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Finer")) {
				_timeGranularity--;
				if (_timeGranularity < 0)
					_timeGranularity = 0;
			}
			if (GUILayout.Button ("Coarser")) {
				_timeGranularity++;
				if (_timeGranularity > MAX_TIME_GRAN)
					_timeGranularity = MAX_TIME_GRAN;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (_timeGranularity == 0) {
				if (GUILayout.Button ("+.01sec")) {
					_UT = _UT + (0.01);
				}
				if (GUILayout.Button ("+.1sec")) {
					_UT = _UT + (0.1);
				}
				if (GUILayout.Button ("+1sec")) {
					_UT = _UT + (1.0);
				}
				if (GUILayout.Button ("+10sec")) {
					_UT = _UT + (10.0);
				}
			} else if (_timeGranularity == 1) {
				if (GUILayout.Button ("+1min")) {
					_UT = _UT + (60.0);
				}
				if (GUILayout.Button ("+10min")) {
					_UT = _UT + (10.0 * 60.0);
				}
				if (GUILayout.Button ("+1hr")) {
					_UT = _UT + (Format.ONE_KHOUR);
				}
				if (GUILayout.Button ("+1d")) {
					_UT = _UT + (Format.ONE_KDAY);
				}
			} else {
				if (GUILayout.Button ("+10d")) {
					_UT = _UT + (10.0 * Format.ONE_KDAY);
				}
				if (GUILayout.Button ("+100d")) {
					_UT = _UT + (100.0 * Format.ONE_KDAY);
				}
				if (GUILayout.Button ("+1yr")) {
					_UT = _UT + (Format.ONE_KYEAR);
				}
				if (GUILayout.Button ("+10yr")) {
					_UT = _UT + (10.0 * Format.ONE_KYEAR);
				}
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (_timeGranularity == 0) {
				if (GUILayout.Button ("-.01sec")) {
					_UT = _UT - (0.01);
				}
				if (GUILayout.Button ("-.1sec")) {
					_UT = _UT - (0.1);
				}
				if (GUILayout.Button ("-1sec")) {
					_UT = _UT - (1.0);
				}
				if (GUILayout.Button ("-10sec")) {
					_UT = _UT - (10.0);
				}
			} else if (_timeGranularity == 1) {
				if (GUILayout.Button ("-1min")) {
					_UT = _UT - (60.0);
				}
				if (GUILayout.Button ("-10min")) {
					_UT = _UT - (10.0 * 60.0);
				}
				if (GUILayout.Button ("-1hr")) {
					_UT = _UT - (Format.ONE_KHOUR);
				}
				if (GUILayout.Button ("-1d")) {
					_UT = _UT - (Format.ONE_KDAY);
				}
			} else {
				if (GUILayout.Button ("-10d")) {
					_UT = _UT - (10.0 * Format.ONE_KDAY);
				}
				if (GUILayout.Button ("-100d")) {
					_UT = _UT - (100.0 * Format.ONE_KDAY);
				}
				if (GUILayout.Button ("-1yr")) {
					_UT = _UT - (Format.ONE_KYEAR);
				}
				if (GUILayout.Button ("-10yr")) {
					_UT = _UT - (10.0 * Format.ONE_KYEAR);
				}
			}
			GUILayout.EndHorizontal ();
		}
	}
}
