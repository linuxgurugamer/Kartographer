/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace Kartographer
{

	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class WarpTo: MonoBehaviour
	{
		static public WarpTo Instance
		{
			get { return _instance; }
		}
		static private WarpTo _instance = null;

		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_totalLabelStyle;
//		private GUIStyle 	_centeredLabelStyle;
//		private GUIStyle 	_rightLabelStyle;
		private GUIStyle	_buttonStyle;
//		private GUIStyle	_scrollStyle;
		private int 		_winID;
		private double 		_UT;
		private double 		_WarpEndUT = 0.0d;
		private Vessel 		_cachedVessel = null;
		private TimeControl _timeControl = new TimeControl();
		private bool		_refreshHeight = false;

		/// <summary>
		/// Awake this instance.
		/// </summary>
		public void Awake() {
			if (_instance)
				Destroy (_instance);
			_instance = this;
		}

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start()
		{
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			_UT = Planetarium.GetUniversalTime ();

			InitStyles ();
		}

		/// <summary>
		/// Callback when this instance is destroyed.
		/// </summary>
		public void OnDestroy()
		{
//			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			ControlUnlock ();
			if (_instance == this)
				_instance = null;
		}

		public void OnGUI()
		{
			if (_active)
			{
				if (_refreshHeight) {
					_refreshHeight = false;
					_windowPos.height = 0.0f;
				}
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Warp To",_windowStyle);
				if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
					_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
					_windowPos.x = Screen.width - _windowPos.width - 50.0f;
				}
				if (_windowPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock();
				}
			}
		}

		/// <summary>
		/// Toggles the window visibility.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
			if (_active) {
				_refreshHeight = true;
			} else {
				Invoke ("ControlUnlock", 1);
			}
		}

		/// <summary>
		/// Lock the Controls.
		/// </summary>
		private void ControlLock()
		{
			InputLockManager.SetControlLock (ControlTypes.TRACKINGSTATION_UI, "Kartograph_WarpTo");
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("Kartograph_WarpTo");
		}

		/// <summary>
		/// Physics update callback.
		/// </summary>
		public void FixedUpdate()
		{
			// Workaround to ensure we stop warping when we should.
			if (_WarpEndUT > 0.0d && Planetarium.GetUniversalTime () > _WarpEndUT+1.0d &&
				TimeWarp.CurrentRateIndex > 0) {
//				Debug.Log ("Warp fix");
				TimeWarp.SetRate (0, true);
				_WarpEndUT = 0.0d;
			}
			// If we stop the warp shut off the workaround.
			if (TimeWarp.CurrentRateIndex == 0 && _WarpEndUT > 0.0d) {
				_WarpEndUT = 0.0d;
			}
		}

		/// <summary>
		/// Build the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical (GUILayout.MinWidth(300.0f));
			GUILayout.Label("Current Time: "+KartographStyle.Instance.GetUTTimeString(Planetarium.GetUniversalTime ()),_labelStyle);
			GUILayout.Label("Warp To:      "+KartographStyle.Instance.GetUTTimeString(_UT),_labelStyle);
			GUILayout.Label("Delta Time:   "+KartographStyle.Instance.GetTimeString(_UT-Planetarium.GetUniversalTime ()),_labelStyle);
			if (_UT < Planetarium.GetUniversalTime ()) {
				_UT = Planetarium.GetUniversalTime ();
			}
			GUILayout.Label ("", _labelStyle);

			Vessel vessel = null;
			Vessel prevVessel = _cachedVessel;
			if (FlightGlobals.ActiveVessel != null) {
				vessel = FlightGlobals.ActiveVessel;
				_cachedVessel = vessel;
			} else if (MapView.fetch != null && MapView.fetch.scaledVessel != null &&
			           MapView.fetch.scaledVessel.vessel != null) {
				vessel = MapView.fetch.scaledVessel.vessel;
				_cachedVessel = vessel;
			} else if (PlanetariumCamera.fetch != null &&
			           PlanetariumCamera.fetch.initialTarget != null &&
			           PlanetariumCamera.fetch.initialTarget.vessel != null) {
				vessel = PlanetariumCamera.fetch.initialTarget.vessel;
				_cachedVessel = vessel;
			} else if (PlanetariumCamera.fetch != null &&
			           PlanetariumCamera.fetch.target != null &&
			           PlanetariumCamera.fetch.target.vessel != null) {
				vessel = PlanetariumCamera.fetch.target.vessel;
				_cachedVessel = vessel;
			} else if (_cachedVessel != null) {
				if (PlanetariumCamera.fetch.target == null) {
					_cachedVessel = null;
				}
				vessel = _cachedVessel;
			}
			if (vessel != prevVessel) {
				_windowPos.height = 0.0f;
			}

			if (vessel != null) {
				GUILayout.Label ("Vessel: "+vessel.RevealName (), _labelStyle);
				if (vessel.orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
					!vessel.Landed) {
					if (GUILayout.Button ("Transition", _buttonStyle)) {
						// Warp to SOI transition.
						_UT = vessel.orbit.EndUT - 10.0d;
					}
				}
				if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
					ManeuverNode maneuver = vessel.patchedConicSolver.maneuverNodes [0];
					double timeToNode = Planetarium.GetUniversalTime () - maneuver.UT;

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Warp To Maneuver", _labelStyle);
					if (GUILayout.Button ("-1m", _buttonStyle) && -timeToNode > Util.ONE_KMIN) {
						_UT = maneuver.UT - Util.ONE_KMIN;
					}
					if (GUILayout.Button ("-10m", _buttonStyle) && -timeToNode > 10.0 * Util.ONE_KMIN) {
						_UT = maneuver.UT - 10.0 * Util.ONE_KMIN;
					}
					if (GUILayout.Button ("-1h", _buttonStyle) && -timeToNode > Util.ONE_KHOUR) {
						_UT = maneuver.UT - Util.ONE_KHOUR;
					}
					if (GUILayout.Button ("-1d", _buttonStyle) && -timeToNode > Util.ONE_KDAY) {
						_UT = maneuver.UT - Util.ONE_KDAY;
					}
					GUILayout.EndHorizontal ();
				}
			}

			_UT = _timeControl.TimeGUI (_UT, vessel);

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("=10min", _buttonStyle)) {
				_UT = Planetarium.GetUniversalTime () + (10.0 * 60.0);
			}
			if (vessel != null) {
				double period = vessel.orbit.period;
				if (GUILayout.Button ("+1 Orbit", _buttonStyle) && period > 0) {
					_UT = _UT + period;
				}
				if (GUILayout.Button ("-1 Orbit", _buttonStyle) && period > 0) {
					_UT = _UT - period;
				}
				if (GUILayout.Button ("+10 Orbit", _buttonStyle) && period > 0) {
					_UT = _UT + (10.0 * period);
				}
				if (GUILayout.Button ("-10 Orbit", _buttonStyle) && period > 0) {
					_UT = _UT - (10.0 * period);
				}
			}
			GUILayout.EndHorizontal ();


			GUILayout.Label ("", _labelStyle);
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Engage", _buttonStyle)) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (_UT);
				_WarpEndUT = _UT;
			}

			if (GUILayout.Button ("Close", _buttonStyle)) {
				// This should close the window since it by definition can only be pressed while visible.
				ToggleWindow ();
			}

			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}



		/// <summary>
		/// Initializes the styles.
		/// </summary>
		public void InitStyles()
		{
			_windowStyle = KartographStyle.Instance.Window;
			if (_totalLabelStyle == null || _labelStyle != KartographStyle.Instance.Label) {
				_totalLabelStyle = new GUIStyle (KartographStyle.Instance.RightLabel);
				_totalLabelStyle.fontStyle = FontStyle.BoldAndItalic;
			}
			_labelStyle = KartographStyle.Instance.Label;
//			_centeredLabelStyle = KartographStyle.Instance.CenteredLabel;
//			_rightLabelStyle = KartographStyle.Instance.RightLabel;
			_buttonStyle = KartographStyle.Instance.Button;
//			_scrollStyle = KartographStyle.Instance.ScrollView;
		}
	}

}

