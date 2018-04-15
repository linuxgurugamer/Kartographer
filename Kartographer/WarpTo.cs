/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using UnityEngine;
using KSP.IO;

using ClickThroughFix;

namespace Kartographer
{

	[KSPAddonImproved (KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class WarpTo : MonoBehaviour
	{
		static public WarpTo Instance {
			get { return _instance; }
		}
		static WarpTo _instance;

		bool _active;
		bool _hidden;
		Rect _windowPos = new Rect ();
		int _winID;
		double _UT;
		double _WarpEndUT;
		Vessel _cachedVessel;
		TimeControl _timeControl = new TimeControl ();

		/// <summary>
		/// Awake this instance.
		/// </summary>
		public void Awake ()
		{
			if (_instance)
				Destroy (_instance);
			_instance = this;
		}

		/// <summary>
		/// Start this instance.
		/// </summary>
		public void Start ()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_windowPos = config.GetValue ("WarpToWindowPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));

			_winID = GUIUtility.GetControlID (FocusType.Passive);
			_UT = Planetarium.GetUniversalTime ();

			GameEvents.onHideUI.Add (Hide);
			GameEvents.onShowUI.Add (UnHide);
			GameEvents.onGamePause.Add (Hide);
			GameEvents.onGameUnpause.Add (UnHide);
		}

		/// <summary>
		/// Callback when this instance is destroyed.
		/// </summary>
		public void OnDestroy ()
		{
			ControlUnlock ();

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("WarpToWindowPos", _windowPos);
			config.save ();

			GameEvents.onHideUI.Remove (Hide);
			GameEvents.onShowUI.Remove (UnHide);
			GameEvents.onGamePause.Remove (Hide);
			GameEvents.onGameUnpause.Remove (UnHide);

			if (_instance == this)
				_instance = null;
		}

		public void Hide ()
		{
			ControlUnlock ();
			_hidden = true;
		}

		public void UnHide ()
		{
			_hidden = false;
		}

		public void OnGUI ()
		{
			if (_active && !_hidden) {
				if (KartographSettings.Instance.UseKspSkin) GUI.skin = HighLogic.Skin;
				_windowPos = ClickThruBlocker.GUILayoutWindow (_winID, _windowPos, OnWindow, "Warp To");
				if (_windowPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock ();
				}
			}
		}

		/// <summary>
		/// Toggles the window visibility.
		/// </summary>
		internal void ToggleWindow ()
		{
			_active = !_active;
			if (!_active) ControlUnlock ();
			_windowPos.width = 0.0f;
			_windowPos.height = 0.0f;
		}

		/// <summary>
		/// Lock the Controls.
		/// </summary>
		void ControlLock ()
		{
			InputLockManager.SetControlLock (ControlTypes.ALLBUTTARGETING, "Kartographer" + name);
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		void ControlUnlock ()
		{
			InputLockManager.RemoveControlLock ("Kartographer" + name);
		}

		/// <summary>
		/// Physics update callback.
		/// </summary>
		public void FixedUpdate ()
		{
			// Workaround to ensure we stop warping when we should.
			if (_WarpEndUT > 0.0d && Planetarium.GetUniversalTime () > _WarpEndUT + 1.0d &&
				TimeWarp.CurrentRateIndex > 0) {
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
		void OnWindow (int windowId)
		{
			GUILayout.BeginVertical (GUILayout.MinWidth (300.0f));
			GUILayout.Label ("Current Time: " + KSPUtil.dateTimeFormatter.PrintDateCompact (Planetarium.GetUniversalTime (), true, true));
			GUILayout.Label ("Warp To:      " + KSPUtil.dateTimeFormatter.PrintDateCompact (_UT, true, true));
			GUILayout.Label ("Delta Time:   " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (_UT - Planetarium.GetUniversalTime (), true, true, true));
			if (_UT < Planetarium.GetUniversalTime ()) {
				_UT = Planetarium.GetUniversalTime ();
			}
			GUILayout.Label ("");

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
				GUILayout.Label ("Vessel: " + vessel.RevealName ());
				if (vessel.orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL &&
					!vessel.Landed) {
					if (GUILayout.Button ("Transition")) {
						// Warp to SOI transition.
						_UT = vessel.orbit.EndUT - 10.0d;
					}
				}
				if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
					ManeuverNode maneuver = vessel.patchedConicSolver.maneuverNodes [0];
					double timeToNode = Planetarium.GetUniversalTime () - maneuver.UT;

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Warp To Maneuver");
					if (GUILayout.Button ("-1m") && -timeToNode > Format.ONE_KMIN) {
						_UT = maneuver.UT - Format.ONE_KMIN;
					}
					if (GUILayout.Button ("-10m") && -timeToNode > 10.0 * Format.ONE_KMIN) {
						_UT = maneuver.UT - 10.0 * Format.ONE_KMIN;
					}
					if (GUILayout.Button ("-1h") && -timeToNode > Format.ONE_KHOUR) {
						_UT = maneuver.UT - Format.ONE_KHOUR;
					}
					if (GUILayout.Button ("-1d") && -timeToNode > Format.ONE_KDAY) {
						_UT = maneuver.UT - Format.ONE_KDAY;
					}
					GUILayout.EndHorizontal ();
				}
			}

			_UT = _timeControl.TimeGUI (_UT, vessel);

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("=10min")) {
				_UT = Planetarium.GetUniversalTime () + (10.0 * 60.0);
			}
			if (vessel != null) {
				double period = vessel.orbit.period;
				if (GUILayout.Button ("+1 Orbit") && period > 0) {
					_UT = _UT + period;
				}
				if (GUILayout.Button ("-1 Orbit") && period > 0) {
					_UT = _UT - period;
				}
				if (GUILayout.Button ("+10 Orbit") && period > 0) {
					_UT = _UT + (10.0 * period);
				}
				if (GUILayout.Button ("-10 Orbit") && period > 0) {
					_UT = _UT - (10.0 * period);
				}
			}
			GUILayout.EndHorizontal ();


			GUILayout.Label ("");
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Engage")) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (_UT);
				_WarpEndUT = _UT;
			}

			if (GUILayout.Button ("Close")) {
				// This should close the window since it by definition can only be pressed while visible.
				ToggleWindow ();
			}

			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
