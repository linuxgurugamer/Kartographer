/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace Kartographer
{
	internal class StoredManeuver
	{
		Vector3d _dv;
		public Vector3d DeltaV {
			get { return _dv; }
		}
		double _UT;
		public double UT {
			get { return _UT; }
		}
		StoredManeuver _next = null;
		public StoredManeuver Next {
			get { return _next; }
			set { _next = value; }
		}


		public double getTotalDeltaV ()
		{
			double next = 0.0d;
			if (_next != null) {
				next = _next.getTotalDeltaV ();
			}
			return _dv.magnitude + next;
		}
		public StoredManeuver (Vector3d dv, double UT, StoredManeuver next = null)
		{
			_dv = dv;
			_UT = UT;
			_next = next;
		}

	}


	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ManeuverEditor : MonoBehaviour
	{
		static public ManeuverEditor Instance {
			get { return _instance; }
		}

		Rect _windowPos = new Rect ();
		Rect _savedPos = new Rect ();
		Vector2 _scrollPos = new Vector2 ();
		bool _maneuverShow;
		bool _hidden;
		ManeuverNode _maneuver;
		Vessel _mvessel;
		int _mindex;
		int _winID;
		int _savedWinID;
		double _increment = 1.0d;
		int _menuSelection = 2;
		bool _minimize;
		List<StoredManeuver> _stored = new List<StoredManeuver> ();
		static ManeuverEditor _instance;
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
			_winID = GUIUtility.GetControlID (FocusType.Passive);
			_savedWinID = GUIUtility.GetControlID (FocusType.Passive);

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();

			_windowPos = config.GetValue ("ManeuverWindowPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));
			GameEvents.onHideUI.Add (Hide);
			GameEvents.onShowUI.Add (UnHide);
			GameEvents.onGamePause.Add (Hide);
			GameEvents.onGameUnpause.Add (UnHide);
		}

		/// <summary>
		/// Destroy this instance.
		/// </summary>
		public void OnDestroy ()
		{
			ControlUnlock ();
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("ManeuverWindowPos", _windowPos);
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
			if (_maneuverShow && !_hidden) {
				if (KartographSettings.Instance.UseKspSkin) GUI.skin = HighLogic.Skin;
				if (_maneuverShow && IsUsable ()) {
					_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Maneuver Editor");
					if (_stored.Count > 0) {
						_savedPos.x = _windowPos.x + _windowPos.width + 10.0f;
						_savedPos.y = _windowPos.y;
						_savedPos = GUILayout.Window (_savedWinID, _savedPos, SavedWindow, "Saved Maneuvers");
					}
				}
				if (_windowPos.Contains (Event.current.mousePosition) | _savedPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock ();
				}
			}
		}

		/// <summary>
		/// Toggles the window.
		/// </summary>
		internal void ToggleWindow ()
		{
			_maneuverShow = !_maneuverShow;
			if (!_maneuverShow) ControlUnlock ();
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
		/// Determines whether the maneuver node editor is allowed.
		/// </summary>
		/// <returns><c>true</c> if this editor is allowed; otherwise, <c>false</c>.</returns>
		public bool IsAllowed ()
		{
			return PSystemSetup.Instance.GetSpaceCenterFacility ("TrackingStation").GetFacilityLevel () > 0 &&
				PSystemSetup.Instance.GetSpaceCenterFacility ("MissionControl").GetFacilityLevel () > 0;
		}

		/// <summary>
		/// Determines whether this instance is usable.
		/// </summary>
		/// <returns><c>true</c> if this instance is usable; otherwise, <c>false</c>.</returns>
		public bool IsUsable ()
		{
			return IsAllowed () && HighLogic.LoadedSceneIsFlight;
		}

		/// <summary>
		/// Restores the maneuvers.
		/// </summary>
		/// <param name="stored">Stored maneuver.</param>
		void RestoreManeuver (StoredManeuver stored)
		{
			DeleteAll ();
			StoredManeuver restore = stored;
			while (restore != null) {
				ManeuverNode node = FlightGlobals.ActiveVessel.patchedConicSolver.AddManeuverNode (restore.UT);
				node.OnGizmoUpdated (restore.DeltaV, restore.UT);
				restore = restore.Next;
			}
		}

		/// <summary>
		/// Draws the window for saved maneuvers.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void SavedWindow (int windowId)
		{
			int i = 0;
			GUILayout.BeginVertical (GUILayout.MinWidth (150.0f));
			if (GUILayout.Button ("Clear All")) {
				_stored.Clear ();
			}
			bool oldMinimize = _minimize;
			_minimize = GUILayout.Toggle (_minimize, "Minimize");
			if (_minimize != oldMinimize) {
				_savedPos.height = 0.0f;
				_savedPos.width = 0.0f;
			}
			if (_minimize) {
				GUILayout.Label ("Saved:" + _stored.Count);
			} else {
				_scrollPos = GUILayout.BeginScrollView (_scrollPos, GUILayout.MinWidth (420.0f), GUILayout.Height (150.0f));
				GUILayout.BeginVertical (GUILayout.Width (380.0f));
				foreach (StoredManeuver stored in _stored) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("" + i, GUILayout.Width (15.0f));
					GUILayout.Label ("Δv:" + Format.GetNumberString (stored.getTotalDeltaV ()) + "m/s", GUILayout.Width (150.0f));


					if (GUILayout.Button ("Delete")) {
						_stored.Remove (stored);
						_savedPos.height = 0.0f;
					}
					if (GUILayout.Button ("Restore") && _stored.Count > 0) {
						DeleteAll ();
						RestoreManeuver (stored);
					}
					GUILayout.EndHorizontal ();
					GUILayout.BeginHorizontal ();
					double timeToNode = Planetarium.GetUniversalTime () - stored.UT;
					GUILayout.Label ("", GUILayout.Width (15.0f));

					GUILayout.Label (" " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (timeToNode, true, true, true), GUILayout.Width (200.0f));
					GUILayout.EndHorizontal ();
					i++;
				}

				GUILayout.EndVertical ();
				GUILayout.EndScrollView ();
			}
			GUILayout.EndVertical ();
		}

		/// <summary>
		/// Deletes all maneuvers.
		/// </summary>
		void DeleteAll ()
		{
			while (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0) {
				FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes [0].RemoveSelf ();
			}
		}
		/// <summary>
		/// Draw the main window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void OnWindow (int windowId)
		{
			if (FlightGlobals.ActiveVessel == null)
				return;
			PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
			GUILayout.BeginVertical (GUILayout.Width (320.0f));
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("New") && IsAllowed ()) {
				_maneuver = solver.AddManeuverNode (Planetarium.GetUniversalTime () + (10.0 * 60.0));
				_mindex = solver.maneuverNodes.IndexOf (_maneuver);
			}
			if (GUILayout.Button ("Delete") && _maneuver != null) {
				_maneuver.RemoveSelf ();
			}
			if (GUILayout.Button ("Delete All") && _maneuver != null) {
				DeleteAll ();
			}
			if (GUILayout.Button ("Store") && solver.maneuverNodes.Count > 0) {
				StoredManeuver start = null;
				StoredManeuver prev = null;
				foreach (ManeuverNode node in solver.maneuverNodes) {
					StoredManeuver temp = new StoredManeuver (node.DeltaV, node.UT);
					if (start == null)
						start = temp;
					if (prev != null)
						prev.Next = temp;
					prev = temp;
				}
				_stored.Add (start);
			}
			if (GUILayout.Button ("Close")) {
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Warp");
			if (GUILayout.Button ("+10m")) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + 10.0 * Format.ONE_KMIN);
			}
			if (GUILayout.Button ("+1h")) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + Format.ONE_KHOUR);
			}
			if (GUILayout.Button ("+1d")) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + Format.ONE_KDAY);
			}
			if (GUILayout.Button ("+10d")) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + 10.0 * Format.ONE_KDAY);
			}
			if (FlightGlobals.ActiveVessel.orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) {
				if (GUILayout.Button ("Transition")) {
					// Cancel any existing warp.
					TimeWarp.SetRate (0, true);
					// Warp to the maneuver.
					TimeWarp.fetch.WarpTo (FlightGlobals.ActiveVessel.orbit.EndUT - Format.ONE_KMIN);
				}
			}
			GUILayout.EndHorizontal ();

			if (solver.maneuverNodes.Count > 0) {
				if (_maneuver == null || _mvessel != FlightGlobals.ActiveVessel ||
					!solver.maneuverNodes.Contains (_maneuver)) {
					_maneuver = solver.maneuverNodes [0];
					_mvessel = FlightGlobals.ActiveVessel;
					_mindex = 0;
				}
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Maneuver:" + (_mindex + 1) + " of " +
					solver.maneuverNodes.Count);
				if (GUILayout.Button ("Prev")) {
					_mindex--;
					if (_mindex < 0)
						_mindex = solver.maneuverNodes.Count - 1;
					_maneuver = solver.maneuverNodes [_mindex];
					_mvessel = FlightGlobals.ActiveVessel;
				}
				if (GUILayout.Button ("Next")) {
					_mindex++;
					if (_mindex >= solver.maneuverNodes.Count)
						_mindex = 0;
					_maneuver = solver.maneuverNodes [_mindex];
					_mvessel = FlightGlobals.ActiveVessel;
				}
				GUILayout.EndHorizontal ();
				if (_maneuver != null) {
					double timeToNode = Planetarium.GetUniversalTime () - _maneuver.UT;
					if (_mindex == 0) {
						GUILayout.BeginHorizontal ();
						GUILayout.Label ("Warp To Maneuver");
						if (GUILayout.Button ("-1m") && -timeToNode > Format.ONE_KMIN) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - Format.ONE_KMIN);
						}
						if (GUILayout.Button ("-10m") && -timeToNode > 10.0 * Format.ONE_KMIN) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - 10.0 * Format.ONE_KMIN);
						}
						if (GUILayout.Button ("-1h") && -timeToNode > Format.ONE_KHOUR) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - Format.ONE_KHOUR);
						}
						GUILayout.EndHorizontal ();
					} else {
						GUILayout.Label ("Warp To Maneuver - Switch to first maneuver");
					}
					GUILayout.Label ("Time:" + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (timeToNode, true, true, true));
					GUILayout.Label ("Δv:" + Format.GetNumberString (_maneuver.DeltaV.magnitude) + "m/s");

					GUILayout.BeginHorizontal ();
					_menuSelection = GUILayout.SelectionGrid (_menuSelection,
						new string [] { ".01 m/s", ".1 m/s", "1 m/s", "10 m/s", "100 m/s", "1000 m/s" }, 3,
						GUILayout.MinWidth (300.0f));
					if (_menuSelection == 0) {
						_increment = 0.01d;
					} else if (_menuSelection == 1) {
						_increment = 0.1d;
					} else if (_menuSelection == 2) {
						_increment = 1.0d;
					} else if (_menuSelection == 3) {
						_increment = 10.0d;
					} else if (_menuSelection == 4) {
						_increment = 100.0d;
					} else if (_menuSelection == 5) {
						_increment = 1000.0d;
					}
					GUILayout.EndHorizontal ();

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Prograde:" + Format.GetNumberString (_maneuver.DeltaV.z) + "m/s",
						GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("-")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("+")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Normal  :" + Format.GetNumberString (_maneuver.DeltaV.y) + "m/s",
						GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("-")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("+")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Radial  :" + Format.GetNumberString (_maneuver.DeltaV.x) + "m/s",
						GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("-")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("+")) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					double ut = _maneuver.UT;
					double utUpdate = _timeControl.TimeGUI (ut, FlightGlobals.ActiveVessel);
					if (utUpdate != ut) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, utUpdate);
					}

					GUILayout.BeginHorizontal ();
					if (GUILayout.Button ("=10min")) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, Planetarium.GetUniversalTime () + (10.0 * 60.0));
					}
					double period = _maneuver.patch.period;
					if (GUILayout.Button ("-10 Orbit") && period > 0 && -timeToNode > 10.0 * period) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT - (10.0 * period));
					}
					if (GUILayout.Button ("-1 Orbit") && period > 0 && -timeToNode > period) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT - period);
					}
					if (GUILayout.Button ("+1 Orbit") && period > 0) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT + period);
					}
					if (GUILayout.Button ("+10 Orbit") && period > 0) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT + (10.0 * period));
					}
					GUILayout.EndHorizontal ();
				} else {
					_windowPos.height = 0;
				}
			} else if (_maneuver != null) {
				_maneuver = null;
				_windowPos.height = 0;
			}

			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
