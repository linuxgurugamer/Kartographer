/*
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace Kartographer
{
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class FocusSelect: MonoBehaviour
	{
		static private FocusSelect _instance;
		static public FocusSelect Instance
		{
			get { return _instance; }
		}

		private Rect 		_windowPos = new Rect();
		private Vector2		_scrollPos = new Vector2();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
//		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private GUIStyle	_toggleStyle;
		private int 		_winID;
		private bool		_active = false;
		private bool 		_bodyList = false;
		private bool 		_refreshHeight = false;
//		private bool 		_targetMode = false;
		private int 		_maneuverCnt = 0;
		private Dictionary<CelestialBody,bool> _expanded = new Dictionary<CelestialBody,bool>();
		private List<CelestialBody> _bodies = new List<CelestialBody>();

		public void Awake() {
			if (_instance)
				Destroy (_instance);
			_instance = this;
		}

		public void Start()
		{
//			Debug.Log ("Focus Select Start");
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();

			_winID = GUIUtility.GetControlID (FocusType.Passive);
			_windowPos = config.GetValue<Rect> ("FocusWindowPos",new Rect());
			_windowPos.width = 0.0f;
			_windowPos.height = 0.0f;

			InitStyles ();
		}

		/// <summary>
		/// Callback for object destruction.
		/// </summary>
		public void OnDestroy()
		{
//			RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("FocusWindowPos",_windowPos);
			config.save ();
			if (_instance == this)
				_instance = null;
		}

        public void OnGUI()
        {
            if (_active)
            {
				if (_active && IsUsable()) {
					_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Focus Select", _windowStyle);
					if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
						_windowPos.y = Screen.height * 0.5f - Math.Max(_windowPos.height * 0.5f,150.0f);
						_windowPos.x = Screen.width * 0.5f - _windowPos.width * 0.5f;
					}
				}
            }
        }

		/// <summary>
		/// Toggles window visibility.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
		}

		public bool IsUsable()
		{
			if (HighLogic.LoadedSceneHasPlanetarium)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			if (_refreshHeight) {
				_refreshHeight = false;
				_windowPos.height = 0.0f;
			}
			int maneuverCount = _maneuverCnt;
			int bodyCount = _bodies.Count;
			_bodies.Clear ();
			GUILayout.BeginVertical (GUILayout.Width(300.0f));
			if (FlightGlobals.ActiveVessel != null) {
				Vessel vessel = FlightGlobals.ActiveVessel;
				GUILayout.Label ("Vessel:", _labelStyle);
				DrawTargetSelectButton (vessel);
				GUILayout.Label ("Bodies on current trajectory:", _labelStyle);
				DrawOrbitBodies(vessel.orbit);

				if (vessel.targetObject != null) {
					GUILayout.Label ("Target:", _labelStyle);
					DrawTargetSelectButton (vessel.targetObject);
				}

				_maneuverCnt = vessel.patchedConicSolver.maneuverNodes.Count;
				if (vessel.patchedConicSolver.maneuverNodes.Count > 0) {
					GUILayout.Label ("Maneuvers:", _labelStyle);
					int i = 1;
					foreach (ManeuverNode node in vessel.patchedConicSolver.maneuverNodes) {
						DrawOrbitBodies (node.nextPatch);
						if (GUILayout.Button ("Maneuver " + i, _buttonStyle)) {
							MapObject map = PlanetariumCamera.fetch.targets.Find (mo => mo.maneuverNode == node);
							if (map != null) {
								if (HighLogic.LoadedSceneIsFlight)
									MapView.EnterMapView();						
								PlanetariumCamera.fetch.SetTarget (map);
							}
						}
						i++;
					}
				}
			}
			bool oldBodyList = _bodyList;
			_bodyList = GUILayout.Toggle (_bodyList, "List Celestial Bodies", _toggleStyle);
			if (oldBodyList != _bodyList)
				_refreshHeight = true;
			if (_bodyList) {
				CelestialBody sun = PSystemManager.Instance.sun.sun;
				_scrollPos = GUILayout.BeginScrollView (_scrollPos, _scrollStyle, GUILayout.MinWidth (300.0f), GUILayout.Height (200.0f));
				GUILayout.BeginVertical ();
				DrawCelestialBodyGUI (sun, 0);
				GUILayout.EndVertical ();
				GUILayout.EndScrollView ();
			}
//			_targetMode = GUILayout.Toggle (_targetMode, "Target Mode", _toggleStyle);
			if (GUILayout.Button ("Close", _buttonStyle)) {
				ToggleWindow ();
			}
			GUILayout.EndVertical ();
			GUI.DragWindow ();
			if (bodyCount != _bodies.Count || _maneuverCnt != maneuverCount)
				_refreshHeight = true;
		}

		private void DrawTargetSelectButton(object ob)
		{
			MapObject map = PlanetariumCamera.fetch.targets.Find (mo => mo.Discoverable.Equals(ob));
			if (map != null) {
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("*", _buttonStyle, GUILayout.Width(20))) {
					FlightGlobals.fetch.SetVesselTarget (map.celestialBody);
				}

				if (GUILayout.Button (map.Discoverable.RevealName (), _buttonStyle)) {
					if (HighLogic.LoadedSceneIsFlight)
						MapView.EnterMapView();						
					PlanetariumCamera.fetch.SetTarget (map);
				}
				GUILayout.EndHorizontal ();
			}
		}

		/// <summary>
		/// Draws the GUI for bodies along the projected orbit.
		/// </summary>
		/// <param name="start">Starting orbit.</param>
		private void DrawOrbitBodies(Orbit start)
		{
			Orbit obt = start;
			while (obt != null) {
				CelestialBody body = obt.referenceBody;
				if (body != null && !_bodies.Contains (body)) {
					DrawTargetSelectButton (body);
					_bodies.Add (body);
				}
				obt = obt.nextPatch;
			}
		}

		/// <summary>
		/// Draws the celestial body GUI.
		/// </summary>
		/// <param name="body">Celestial Body.</param>
		/// <param name="depth">Depth.</param>
		private void DrawCelestialBodyGUI(CelestialBody body, int depth)
		{
			if (!_expanded.ContainsKey (body)) {
				_expanded.Add (body, false);
			}
			bool nextLevel = body.orbitingBodies.Count > 0;
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", _labelStyle, GUILayout.Width (20.0f * depth));
			DrawTargetSelectButton (body);
			if (nextLevel) {
				if (GUILayout.Button (_expanded [body] ? "-" : "+", _buttonStyle,GUILayout.Width(20.0f))) {
					_expanded [body] = !(_expanded [body]);
				}
			}
			GUILayout.EndHorizontal ();
			if (_expanded [body]) {
				foreach (CelestialBody child in body.orbitingBodies) {
					DrawCelestialBodyGUI (child, depth + 1);
				}
			}
		}

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		public void InitStyles() {
			_windowStyle = KartographStyle.Instance.Window;
			_labelStyle = KartographStyle.Instance.Label;
//			_centeredLabelStyle = KartographStyle.Instance.CenteredLabel;
			_buttonStyle = KartographStyle.Instance.Button;
			_scrollStyle = KartographStyle.Instance.ScrollView;
			_toggleStyle = KartographStyle.Instance.Toggle;
		}

	}
}

