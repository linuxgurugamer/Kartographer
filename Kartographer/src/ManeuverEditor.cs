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
	internal class StoredManeuver
	{
		private Vector3d _dv;
		public Vector3d DeltaV
		{
			get { return _dv; }
		}
		private double	 _UT;
		public double UT
		{
			get { return _UT; }
		}
		private StoredManeuver _next = null;
		public StoredManeuver Next {
			get { return _next; }
			set { _next = value; }
		}


		public double getTotalDeltaV()
		{
			double next = 0.0d;
			if (_next != null) {
				next = _next.getTotalDeltaV ();
			}
			return _dv.magnitude + next;
		}
		public StoredManeuver(Vector3d dv, double UT, StoredManeuver next = null)
		{
			_dv = dv;
			_UT = UT;
			_next = next;
		}

	}


	[KSPAddon(KSPAddon.Startup.Flight,false)]
	public class ManeuverEditor: MonoBehaviour
	{
		static public ManeuverEditor Instance
		{
			get { return _instance; }
		}

		private Rect 		_windowPos = new Rect();
		private Rect 		_savedPos = new Rect();
		private Vector2		_scrollPos = new Vector2();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
//		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private GUIStyle	_toggleStyle;
		private bool		_maneuverShow = false;
		private ManeuverNode _maneuver = null;
		private Vessel		_mvessel = null;
		private int			_mindex = 0;
		private int 		_winID;
		private int 		_savedWinID;
		private double		_increment = 1.0d;
		private int 		_menuSelection = 2;
		private bool		_minimize = false;
		private List<StoredManeuver> _stored = new List<StoredManeuver>();
		static private ManeuverEditor _instance;
		private TimeControl _timeControl = new TimeControl();

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
			_savedWinID = GUIUtility.GetControlID (FocusType.Passive);
//			Debug.Log ("Maneuver Editor Start");

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();

			_windowPos = config.GetValue<Rect> ("ManeuverWindowPos",new Rect());
			_windowPos.width = 0.0f;
			_windowPos.height = 0.0f;

			InitStyles ();
		}

		/// <summary>
		/// Destroy this instance.
		/// </summary>
		public void OnDestroy()
		{
//			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("ManeuverWindowPos",_windowPos);
			config.save ();
			if (_instance == this)
				_instance = null;
		}

		public void OnGUI()
		{
			if (_maneuverShow)
			{
				if (_maneuverShow && IsUsable()) {
					_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Maneuver Editor", _windowStyle);
					if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
						_windowPos.y = Screen.height * 0.5f - Math.Max(_windowPos.height * 0.5f,200.0f);
						_windowPos.x = 50.0f;
					}
					if (_stored.Count > 0) {
						_savedPos.x = _windowPos.x + _windowPos.width + 10.0f;
						_savedPos.y = _windowPos.y;
						_savedPos = GUILayout.Window (_savedWinID, _savedPos, SavedWindow, "Saved Maneuvers", _windowStyle);
					}
				}
			}
		}

		/// <summary>
		/// Toggles the window.
		/// </summary>
		public void ToggleWindow()
		{
			_maneuverShow = !_maneuverShow;
			if (!_maneuverShow) {
				Invoke ("ControlUnlock", 1);
			}
		}

		/// <summary>
		/// Determines whether the maneuver node editor is allowed.
		/// </summary>
		/// <returns><c>true</c> if this editor is allowed; otherwise, <c>false</c>.</returns>
		public bool IsAllowed()
		{
			return PSystemSetup.Instance.GetSpaceCenterFacility ("TrackingStation").GetFacilityLevel () > 0 &&
				PSystemSetup.Instance.GetSpaceCenterFacility ("MissionControl").GetFacilityLevel () > 0;
		}

		/// <summary>
		/// Determines whether this instance is usable.
		/// </summary>
		/// <returns><c>true</c> if this instance is usable; otherwise, <c>false</c>.</returns>
		public bool IsUsable()
		{
			return IsAllowed() && HighLogic.LoadedSceneIsFlight && MapView.MapIsEnabled;
		}

		/// <summary>
		/// Restores the maneuvers.
		/// </summary>
		/// <param name="stored">Stored maneuver.</param>
		private void RestoreManeuver(StoredManeuver stored)
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
		private void SavedWindow(int windowId)
		{
			int i = 0;
			GUILayout.BeginVertical (GUILayout.MinWidth(150.0f));
			if (GUILayout.Button ("Clear All", _buttonStyle)) {
				_stored.Clear ();
			}
			bool oldMinimize = _minimize;
			_minimize = GUILayout.Toggle (_minimize, "Minimize",_toggleStyle);
			if (_minimize != oldMinimize) {
				_savedPos.height = 0.0f;
				_savedPos.width = 0.0f;
			}
			if (_minimize) {
				GUILayout.Label ("Saved:" + _stored.Count, _labelStyle);
			} else {
				_scrollPos = GUILayout.BeginScrollView (_scrollPos, _scrollStyle, GUILayout.MinWidth (420.0f), GUILayout.Height (150.0f));
				GUILayout.BeginVertical (GUILayout.Width (380.0f));
				foreach (StoredManeuver stored in _stored) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("" + i, _labelStyle, GUILayout.Width (15.0f));
					GUILayout.Label ("Δv:" + KartographStyle.Instance.GetNumberString (stored.getTotalDeltaV ()) + "m/s", _labelStyle, GUILayout.Width (150.0f));


					if (GUILayout.Button ("Delete", _buttonStyle)) {
						_stored.Remove (stored);
						_savedPos.height = 0.0f;
					}
					if (GUILayout.Button ("Restore", _buttonStyle) && _stored.Count > 0) {
						DeleteAll ();
						RestoreManeuver (stored);
					}
					GUILayout.EndHorizontal ();
					GUILayout.BeginHorizontal ();
					double timeToNode = Planetarium.GetUniversalTime () - stored.UT;
					GUILayout.Label ("", _labelStyle, GUILayout.Width (15.0f));

					GUILayout.Label (" " + KartographStyle.Instance.GetTimeString (timeToNode), _labelStyle, GUILayout.Width (200.0f));
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
		private void DeleteAll()
		{
			while (FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes.Count > 0) {
				FlightGlobals.ActiveVessel.patchedConicSolver.maneuverNodes[0].RemoveSelf ();
			}			
		}
		/// <summary>
		/// Draw the main window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			if (FlightGlobals.ActiveVessel == null)
				return;
			PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;
			GUILayout.BeginVertical (GUILayout.Width(320.0f));
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("New",_buttonStyle) && IsAllowed()) {
				_maneuver = solver.AddManeuverNode(Planetarium.GetUniversalTime() + (10.0 * 60.0));
				_mindex = solver.maneuverNodes.IndexOf (_maneuver);
			}
			if (GUILayout.Button ("Delete",_buttonStyle) && _maneuver != null) {
				_maneuver.RemoveSelf ();
			}
			if (GUILayout.Button ("Delete All",_buttonStyle) && _maneuver != null) {
				DeleteAll();
			}
			if (GUILayout.Button ("Store",_buttonStyle) && solver.maneuverNodes.Count > 0) {
				StoredManeuver start = null;
				StoredManeuver prev = null;
				foreach (ManeuverNode node in solver.maneuverNodes)
				{
					StoredManeuver temp = new StoredManeuver(node.DeltaV, node.UT);
					if (start == null)
						start = temp;
					if (prev != null)
						prev.Next = temp;
					prev = temp;
				}
				_stored.Add (start);
			}
			if (GUILayout.Button ("Close",_buttonStyle)) {
				_maneuverShow = false;
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Warp", _labelStyle);
			if (GUILayout.Button ("+10m", _buttonStyle)) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + 10.0*Util.ONE_KMIN);
			}
			if (GUILayout.Button ("+1h", _buttonStyle)) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + Util.ONE_KHOUR);
			}
			if (GUILayout.Button ("+1d", _buttonStyle)) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + Util.ONE_KDAY);
			}
			if (GUILayout.Button ("+10d", _buttonStyle)) {
				// Cancel any existing warp.
				TimeWarp.SetRate (0, true);
				// Warp to the maneuver.
				TimeWarp.fetch.WarpTo (Planetarium.GetUniversalTime () + 10.0*Util.ONE_KDAY);
			}
			if (FlightGlobals.ActiveVessel.orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL) {
				if (GUILayout.Button ("Transition", _buttonStyle)) {
					// Cancel any existing warp.
					TimeWarp.SetRate (0, true);
					// Warp to the maneuver.
					TimeWarp.fetch.WarpTo (FlightGlobals.ActiveVessel.orbit.EndUT - Util.ONE_KMIN);
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
					solver.maneuverNodes.Count, _labelStyle);
				if (GUILayout.Button ("Next", _buttonStyle)) {
					_mindex++;
					if (_mindex >= solver.maneuverNodes.Count)
						_mindex = 0;
					_maneuver = solver.maneuverNodes [_mindex];
					_mvessel = FlightGlobals.ActiveVessel;
				}
				if (GUILayout.Button ("Prev", _buttonStyle)) {
					_mindex--;
					if (_mindex < 0)
						_mindex = solver.maneuverNodes.Count - 1;
					_maneuver = solver.maneuverNodes [_mindex];
					_mvessel = FlightGlobals.ActiveVessel;
				}
				GUILayout.EndHorizontal ();
				if (_maneuver != null) {
					double timeToNode = Planetarium.GetUniversalTime () - _maneuver.UT;
					if (_mindex == 0) {
						GUILayout.BeginHorizontal ();
						GUILayout.Label ("Warp To Maneuver", _labelStyle);
						if (GUILayout.Button ("-1m", _buttonStyle) && -timeToNode > Util.ONE_KMIN) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - Util.ONE_KMIN);
						}
						if (GUILayout.Button ("-10m", _buttonStyle) && -timeToNode > 10.0 * Util.ONE_KMIN) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - 10.0 * Util.ONE_KMIN);
						}
						if (GUILayout.Button ("-1h", _buttonStyle) && -timeToNode > Util.ONE_KHOUR) {
							// Cancel any existing warp.
							TimeWarp.SetRate (0, true);
							// Warp to the maneuver.
							TimeWarp.fetch.WarpTo (_maneuver.UT - Util.ONE_KHOUR);
						}
						GUILayout.EndHorizontal ();
					} else {
						GUILayout.Label ("Warp To Maneuver - Switch to first maneuver", _labelStyle);
					}
					GUILayout.Label ("Time:" + KartographStyle.Instance.GetTimeString (timeToNode), _labelStyle);
					GUILayout.Label ("Δv:" + KartographStyle.Instance.GetNumberString (_maneuver.DeltaV.magnitude) + "m/s", _labelStyle);

					GUILayout.BeginHorizontal ();
					_menuSelection = GUILayout.SelectionGrid (_menuSelection, 
						new string[]{ ".01 m/s", ".1 m/s", "1 m/s", "10 m/s", "100 m/s", "1000 m/s" }, 3, _buttonStyle,
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
					GUILayout.Label ("Prograde:" + KartographStyle.Instance.GetNumberString (_maneuver.DeltaV.z) + "m/s",
						_labelStyle, GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("+", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("-", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.z = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Normal  :" + KartographStyle.Instance.GetNumberString (_maneuver.DeltaV.y) + "m/s",
						_labelStyle, GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("+", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("-", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.y = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Radial  :" + KartographStyle.Instance.GetNumberString (_maneuver.DeltaV.x) + "m/s",
						_labelStyle, GUILayout.MinWidth (200.0f));
					if (GUILayout.Button ("+", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x += _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("-", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x -= _increment;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					if (GUILayout.Button ("0", _buttonStyle)) {
						Vector3d dv = _maneuver.DeltaV;
						dv.x = 0.0d;
						_maneuver.OnGizmoUpdated (dv, _maneuver.UT);
					}
					GUILayout.EndHorizontal ();

					double ut = _maneuver.UT;
					double utUpdate = _timeControl.TimeGUI (ut, FlightGlobals.ActiveVessel);
					if (utUpdate != ut) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, utUpdate);
					}

					GUILayout.BeginHorizontal ();
					if (GUILayout.Button ("=10min", _buttonStyle)) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, Planetarium.GetUniversalTime () + (10.0 * 60.0));
					}
					double period = _maneuver.patch.period;
					if (GUILayout.Button ("+1 Orbit", _buttonStyle) && period > 0) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT + period);
					}
					if (GUILayout.Button ("-1 Orbit", _buttonStyle) && period > 0 && -timeToNode > period) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT - period);
					}
					if (GUILayout.Button ("+10 Orbit", _buttonStyle) && period > 0) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT + (10.0 * period));
					}
					if (GUILayout.Button ("-10 Orbit", _buttonStyle) && period > 0 && -timeToNode > 10.0*period) {
						_maneuver.OnGizmoUpdated (_maneuver.DeltaV, _maneuver.UT - (10.0 * period));
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

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		public void InitStyles()
		{
			_windowStyle = KartographStyle.Instance.Window;
			_labelStyle = KartographStyle.Instance.Label;
//			_centeredLabelStyle = KartographStyle.Instance.CenteredLabel;
			_buttonStyle = KartographStyle.Instance.Button;
			_scrollStyle = KartographStyle.Instance.ScrollView;
			_toggleStyle = KartographStyle.Instance.Toggle;
		}

	}
}