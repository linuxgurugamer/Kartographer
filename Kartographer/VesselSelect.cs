/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

using ClickThroughFix;

namespace Kartographer
{
	[KSPAddonImproved (KSPAddonImproved.Startup.Flight, false)]
	public class VesselSelect : MonoBehaviour
	{
		static VesselSelect _instance;
		static public VesselSelect Instance {
			get { return _instance; }
		}
		const double SAMPLE_TIME = 1.0d;

		Rect _windowPos = new Rect ();
		Vector2 _scrollPos = new Vector2 ();
		int _winID;
		bool _active;
		bool _hidden;
		double _prevTime;
		bool _asteroids;
		bool _debris;
		Vessel _krakenSacrifice;
		int _krakenCountDown;
		bool _krakenWarn;
		List<Vessel> _vessels = new List<Vessel> ();
		VesselComparer.CompareType _vesselCmpr = VesselComparer.CompareType.DISTANCE;
		bool _ascend = true;


		class VesselComparer : IComparer<Vessel>
		{
			public enum CompareType
			{
				DISTANCE,
				MASS,
				NAME
			}
			CompareType _type;
			bool _ascend;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Kartographer.VesselSelect.VesselComparer"/> class.
			/// </summary>
			/// <param name="ascend">If set to <c>true</c> ascend.</param>
			/// <param name="type">Type.</param>
			public VesselComparer (bool ascend = true, CompareType type = CompareType.DISTANCE)
			{
				_type = type;
				_ascend = ascend;
			}

			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The x vessel.</param>
			/// <param name="y">The y vessel.</param>
			public int Compare (Vessel x, Vessel y)
			{
				if (!_ascend) {
					Vessel tmp = y;
					y = x;
					x = tmp;
				}
				switch (_type) {
				case CompareType.DISTANCE: {
						Vessel vessel = FlightGlobals.ActiveVessel;
						double distancex = Vector3.Distance (x.transform.position, vessel.transform.position);
						double distancey = Vector3.Distance (y.transform.position, vessel.transform.position);
						return distancex.CompareTo (distancey);
					}
				case CompareType.NAME:
					return x.RevealName ().CompareTo (y.RevealName ());
				case CompareType.MASS:
					return x.RevealMass ().CompareTo (y.RevealMass ());
				}
				return x.RevealName ().CompareTo (y.RevealName ());
			}
		}


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
			_windowPos = config.GetValue ("VesselWindowPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));

			_krakenWarn = config.GetValue ("KrakenWarn", false);

			GameEvents.onVesselDestroy.Add (VesselDestroyed);
			GameEvents.onHideUI.Add (Hide);
			GameEvents.onShowUI.Add (UnHide);
			GameEvents.onGamePause.Add (Hide);
			GameEvents.onGameUnpause.Add (UnHide);
		}

		/// <summary>
		/// Callback for object destruction.
		/// </summary>
		public void OnDestroy ()
		{
			ControlUnlock ();

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("VesselWindowPos", _windowPos);
			config.SetValue ("KrakenWarn", _krakenWarn);
			config.save ();

			GameEvents.onVesselDestroy.Remove (VesselDestroyed);
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
				_windowPos = ClickThruBlocker.GUILayoutWindow (_winID, _windowPos, OnWindow, "Vessel Select");
				if (_windowPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock ();
				}
			}
		}

		/// <summary>
		/// Toggles window visibility.
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

		public bool IsUsable ()
		{
			return true;
		}

		/// <summary>
		/// Callback when a vessel is destroyed.
		/// </summary>
		/// <param name="vessel">Vessel.</param>
		public void VesselDestroyed (Vessel vessel)
		{
			_prevTime = 0.0d;
		}

		/// <summary>
		/// Called on physics update. Set the Distances.
		/// </summary>
		public void FixedUpdate ()
		{
			double time = Planetarium.GetUniversalTime ();

			if (!_active || time - _prevTime < SAMPLE_TIME) {
				return;
			}

			_prevTime = time;

			_vessels.Clear ();
			Vessel vessel = FlightGlobals.ActiveVessel;
			foreach (Vessel v in FlightGlobals.Vessels) {
				if (v == vessel)
					continue;
				_vessels.Add (v);
			}
			_vessels.Sort (new VesselComparer (_ascend, _vesselCmpr));

			// Destroy a random leaf part if kraken is set.
			if (_krakenSacrifice != null) {

				if (_krakenCountDown == 0) {
					int count = _krakenSacrifice.parts.Count;
					Part part = _krakenSacrifice.parts [Random.Range (0, count - 1)];
					while (part.children.Count > 0) {
						part = part.children [Random.Range (0, part.children.Count - 1)];
					}
					part.explode ();
					_krakenCountDown = Random.Range (2, 10);
				} else {
					_krakenCountDown--;
				}

				if (_krakenSacrifice.parts.Count == 0) {
					_krakenSacrifice = null;
				}
			}
		}

		/// <summary>
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void OnWindow (int windowId)
		{
			GUILayout.BeginVertical ();
			if (FlightGlobals.ActiveVessel != null) {
				GUILayout.Label (FlightGlobals.ActiveVessel.RevealName ()/*, _centeredLabelStyle*/);
			}
			_asteroids = GUILayout.Toggle (_asteroids, "Include Asteroids");
			_debris = GUILayout.Toggle (_debris, "Include Debris");

			GUILayout.BeginHorizontal (GUILayout.MinWidth (420.0f));
			if (GUILayout.Button ("Name", GUILayout.MinWidth (100.0f))) {
				if (_vesselCmpr == VesselComparer.CompareType.NAME)
					_ascend = !_ascend;
				else
					_ascend = true;
				_vesselCmpr = VesselComparer.CompareType.NAME;
				_prevTime = 0.0d;
			}
			if (GUILayout.Button ("Distance", GUILayout.MinWidth (100.0f))) {
				if (_vesselCmpr == VesselComparer.CompareType.DISTANCE)
					_ascend = !_ascend;
				else
					_ascend = true;
				_vesselCmpr = VesselComparer.CompareType.DISTANCE;
				_prevTime = 0.0d;
			}
			GUILayout.EndHorizontal ();

			_scrollPos = GUILayout.BeginScrollView (_scrollPos, GUILayout.MinWidth (420.0f), GUILayout.Height (200.0f));
			Vessel vessel = FlightGlobals.ActiveVessel;
			foreach (Vessel v in _vessels) {
				string desc = "";
				if (v.vesselType == VesselType.Debris && !_debris) {
					continue;
				}
				if (v.vesselType == VesselType.SpaceObject && !_asteroids) {
					continue;
				}
				if (v.vesselType == VesselType.SpaceObject && v.DiscoveryInfo.Level < DiscoveryLevels.Name) {
					continue;
				}
				if (v.vesselType == VesselType.Flag) {
					desc = "Flag:";
				}

				GUILayout.BeginHorizontal ();
				GUILayout.Label (desc + v.RevealName (), GUILayout.MinWidth (150.0f));

				double distance = 0.0d;
				if (vessel != null) {
					distance = Vector3.Distance (v.transform.position, vessel.transform.position);
					GUILayout.Label (Format.GetNumberString (distance) + "m", GUILayout.MinWidth (100.0f));
				}
				if (GUILayout.Button ("Target")) {
					FlightGlobals.fetch.SetVesselTarget (v);
				}
				if (GUILayout.Button ("Switch")) {
					FlightGlobals.SetActiveVessel (v);
				}

				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (!KartographSettings.Instance.DisableKraken && GUILayout.Button ("Unleash the Kraken")) {
				if (_krakenWarn) {
					_krakenSacrifice = FlightGlobals.ActiveVessel;
				} else {
					ScreenMessages.PostScreenMessage ("Unleashing the Kraken will destroy the active vessel. You get only one warning.");
					_krakenWarn = true;
				}
			}
			if (GUILayout.Button ("Untarget")) {
				FlightGlobals.fetch.SetVesselTarget (null);
			}
			if (GUILayout.Button ("Close")) {
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
