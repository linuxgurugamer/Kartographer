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
	[KSPAddonImproved (KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class CelestialBodyData : MonoBehaviour
	{
		static public CelestialBodyData Instance {
			get { return _instance; }
		}
		static CelestialBodyData _instance;

		bool _active;
		bool _hidden;
		Rect _windowPos = new Rect ();
		int _winID;
		Vector2 _scrollPos = new Vector2 ();
		Dictionary<CelestialBody, bool> _expanded = new Dictionary<CelestialBody, bool> ();
		CelestialBody _body = null;

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

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_windowPos = config.GetValue ("CelestialBodiesWindowPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));

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
			config.SetValue ("CelestialBodiesWindowPos", _windowPos);
			config.save ();

			GameEvents.onHideUI.Remove (Hide);
			GameEvents.onShowUI.Remove (UnHide);
			GameEvents.onGamePause.Remove (Hide);
			GameEvents.onGameUnpause.Remove (UnHide);

			if (_instance == this)
				_instance = null;
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
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Celestial Body Data");
				if (_windowPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock ();
				}
			}
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
		/// Build the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void OnWindow (int windowId)
		{
			GUILayout.BeginHorizontal ();
			RenderBodyList ();
			if (_body != null) {
				GUILayout.Space (10);
				RenderBodyStats ();
				GUILayout.Space (10);
				RenderBodyOrbitStats ();
			}
			GUILayout.EndHorizontal ();
			GUI.DragWindow ();
		}

		void RenderBodyList ()
		{
			//CelestialBody sun = PSystemManager.Instance.sun.sun;
			CelestialBody centralBody = FlightGlobals.Bodies [0];
			// Create with the central body intially expanded
			if (!_expanded.ContainsKey (centralBody)) {
				_expanded.Add (centralBody, true);
			}
			GUILayout.BeginVertical (GUILayout.MinWidth (300.0f));
			_scrollPos = GUILayout.BeginScrollView (_scrollPos, GUILayout.Height (300.0f));
			DrawCelestialBodyGUI (centralBody, 0);
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("View")) {
				if (_body != null) {
					if (HighLogic.LoadedSceneHasPlanetarium)
						MapView.EnterMapView ();
					PlanetariumCamera.fetch.SetTarget (_body);
				}
			}
			if (GUILayout.Button ("Target")) {
				if (_body != null && FlightGlobals.ActiveVessel != null) {
					FlightGlobals.fetch.SetVesselTarget (_body);
				}
			}
			GUILayout.EndHorizontal ();
			if (GUILayout.Button ("Close")) {
				ToggleWindow ();
			}
			GUILayout.EndVertical ();
		}

		/// <summary>
		/// Draws the celestial body GUI.
		/// </summary>
		/// <param name="body">Celestial Body.</param>
		/// <param name="depth">Depth.</param>
		void DrawCelestialBodyGUI (CelestialBody body, int depth)
		{
			if (!_expanded.ContainsKey (body)) {
				_expanded.Add (body, false);
			}
			bool nextLevel = body.orbitingBodies.Count > 0;
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (24.0f * depth));
			if (nextLevel) {
				if (GUILayout.Button (_expanded [body] ? "-" : "+", GUILayout.Width (20.0f))) {
					_expanded [body] = !(_expanded [body]);
				}
			}
			if (_body == body) GUI.contentColor = Color.yellow;
			if (GUILayout.Button (body.displayName.Replace ("^N", ""))) { //??????
				_body = body;
				_windowPos.height = 0.0f;
				_windowPos.width = 0.0f;
			}
			GUI.contentColor = Color.white;
			GUILayout.EndHorizontal ();
			if (_expanded [body]) {
				foreach (CelestialBody child in body.orbitingBodies) {
					DrawCelestialBodyGUI (child, depth + 1);
				}
			}
		}

		void RenderBodyStats ()
		{
			GUILayout.BeginVertical (GUILayout.Width (250));
			GUILayout.Label ("Celestial Body: " + _body.displayName.Replace ("^N", ""));
			GUILayout.Label ("Surface Gravity: " +
							 (_body.gMagnitudeAtCenter / (Math.Pow (_body.Radius, 2))).ToString ("G3") + " m/s²");
			GUILayout.Label ("Mass: " + Format.GetNumberString (_body.Mass) + "g");
			GUILayout.Label ("GM (μ): " + _body.gravParameter.ToString ("e6") + " m³/s²");
			GUILayout.Label ("Radius: " + Format.GetNumberString (_body.Radius) + "m");
			GUILayout.Label ("Rotation Period: " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (_body.rotationPeriod, true, true, true));
			GUILayout.Label ("Tidally locked: " + (_body.tidallyLocked ? "Yes" : "No"));
			if (_body.atmosphere) {
				GUILayout.Label ("Atmosphere");
				GUILayout.Label ("Oxygen:" + (_body.atmosphereContainsOxygen ? "Yes" : "No"));
				GUILayout.Label ("Height:" + Format.GetNumberString (_body.atmosphereDepth) + "m");
			}
			GUILayout.EndVertical ();
		}

		void RenderBodyOrbitStats ()
		{
			if (_body.orbit == null) return;
			GUILayout.BeginVertical (GUILayout.Width (300));
			if (_body.orbit.referenceBody != null) {
				GUILayout.Label ("Reference Body: " + _body.orbit.referenceBody.displayName.Replace ("^N", ""));
			}
			GUILayout.Label ("Apoapsis: " + Format.GetNumberString (_body.orbit.ApA) + "m");
			GUILayout.Label ("Periapsis: " + Format.GetNumberString (_body.orbit.PeA) + "m");
			GUILayout.Label ("Semi-major Axis: " + Format.GetNumberString (_body.orbit.semiMajorAxis) + "m");
			GUILayout.Label ("Semi-minor Axis: " + Format.GetNumberString (_body.orbit.semiMinorAxis) + "m");
			GUILayout.Label ("Semi Latus Rectum: " + Format.GetNumberString (_body.orbit.semiLatusRectum) + "m");
			GUILayout.Label ("Inclination: " + _body.orbit.inclination.ToString ("0.00##") + "°");
			GUILayout.Label ("Longitude of AN: " + _body.orbit.LAN.ToString ("0.00##") + "°");
			GUILayout.Label ("Argument of Periapsis: " + _body.orbit.argumentOfPeriapsis.ToString ("0.00##") + "°");
			GUILayout.Label ("Time to Apoapsis: " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (_body.orbit.timeToAp, true, true, true));
			GUILayout.Label ("Time to Periapsis: " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (_body.orbit.timeToPe, true, true, true));
			GUILayout.Label ("Orbital Period: " + KSPUtil.dateTimeFormatter.PrintDateDeltaCompact (_body.orbit.period, true, true, true));
			GUILayout.Label ("Orbital Speed: " + Format.GetNumberString (_body.orbit.vel.magnitude) + "m/s");
			GUILayout.Label ("Eccentricity: " + _body.orbit.eccentricity.ToString ("g4"));
			GUILayout.EndVertical ();
		}
	}
}
