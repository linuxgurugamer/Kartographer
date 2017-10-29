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
	[KSPAddon(KSPAddon.Startup.Flight,false)]
	public class VesselSelect: MonoBehaviour
	{
		static private VesselSelect _instance;
		static public VesselSelect Instance
		{
			get { return _instance; }
		}
		private const double 	SAMPLE_TIME = 1.0d;

		private Rect 		_windowPos = new Rect();
		private Vector2		_scrollPos = new Vector2();
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private GUIStyle	_scrollStyle;
		private GUIStyle	_toggleStyle;
		private int 		_winID;
		private bool		_active = false;
		private double 		_prevTime 		= 0.0d;
		private bool 		_asteroids = false;
		private bool 		_debris = false;
		private Vessel 		_krakenSacrifice = null;
		private int 		_krakenCountDown = 0;
		private bool 		_krakenWarn = false;
		private List<Vessel> _vessels = new List<Vessel>();
		private VesselComparer.CompareType _vesselCmpr = VesselComparer.CompareType.DISTANCE;
		private bool		_ascend = true;


		class VesselComparer: IComparer<Vessel>
		{
			public enum CompareType
			{
				DISTANCE,
				MASS,
				NAME
			}
			private CompareType _type;
			private bool 		_ascend;

			/// <summary>
			/// Initializes a new instance of the <see cref="Kartographer.VesselSelect+VesselComparer"/> class.
			/// </summary>
			/// <param name="ascend">If set to <c>true</c> sort ascending order.</param>
			/// <param name="type">Type of sorting.</param>
			public VesselComparer(bool ascend = true, CompareType type=CompareType.DISTANCE)
			{
				_type = type;
				_ascend = ascend;
			}

			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The x vessel.</param>
			/// <param name="y">The y vessel.</param>
			public int Compare(Vessel x, Vessel y)
			{
				if (!_ascend) {
					Vessel tmp = y;
					y = x;
					x = tmp;
				}
				switch (_type) {
				case CompareType.DISTANCE:
					{
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
//			Debug.Log ("Vessel Select Start");
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_windowPos = config.GetValue<Rect> ("VesselWindowPos",new Rect());

			_windowPos.width = 0.0f;
			_windowPos.height = 0.0f;

			_krakenWarn = config.GetValue<bool> ("KrakenWarn",false);

			GameEvents.onVesselDestroy.Add (VesselDestroyed);
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
			config.SetValue ("VesselWindowPos",_windowPos);
			config.SetValue ("KrakenWarn", _krakenWarn);
			config.save ();

			GameEvents.onVesselDestroy.Remove (VesselDestroyed);

			if (_instance == this)
				_instance = null;
		}

		public void OnGUI()
		{
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Vessel Select", _windowStyle);
				if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
					_windowPos.y = Screen.height * 0.5f - Math.Max(_windowPos.height * 0.5f,150.0f);
					_windowPos.x = Screen.width * 0.5f - _windowPos.width * 0.5f;
				}
			}
		}

		/// <summary>
		/// Toggles window visibility.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
			if (_active) {
//				RenderingManager.AddToPostDrawQueue (0, OnDraw);
			} else {
//				RenderingManager.RemoveFromPostDrawQueue (0, OnDraw);
			}
		}

		public bool IsUsable()
		{
			return true;
		}

		/// <summary>
		/// Callback when a vessel is destroyed.
		/// </summary>
		/// <param name="vessel">Vessel.</param>
		public void VesselDestroyed(Vessel vessel)
		{
			_prevTime = 0.0d;
		}

		/// <summary>
		/// Called on physics update. Set the Distances.
		/// </summary>
		public void FixedUpdate()
		{
			double time = Planetarium.GetUniversalTime ();

			if (!_active || HighLogic.LoadedSceneIsEditor || time - _prevTime < SAMPLE_TIME) {
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
					Part part = _krakenSacrifice.parts [UnityEngine.Random.Range (0, count - 1)];
					while (part.children.Count > 0) {
						part = part.children [UnityEngine.Random.Range (0, part.children.Count - 1)];
					}
					part.explode ();
					_krakenCountDown = UnityEngine.Random.Range(2, 10);
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
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical ();
			if (FlightGlobals.ActiveVessel != null) {
				GUILayout.Label (FlightGlobals.ActiveVessel.RevealName (), _centeredLabelStyle);
			}
			_asteroids = GUILayout.Toggle (_asteroids, "Include Asteroids", _toggleStyle);
			_debris = GUILayout.Toggle (_debris, "Include Debris", _toggleStyle);

			GUILayout.BeginHorizontal (GUILayout.MinWidth(420.0f));
			if (GUILayout.Button ("Name", _buttonStyle,GUILayout.MinWidth(100.0f))) {
				if (_vesselCmpr ==  VesselComparer.CompareType.NAME)
					_ascend = !_ascend;
				else
					_ascend = true;
				_vesselCmpr = VesselComparer.CompareType.NAME;
				_prevTime = 0.0d;
			}
			if (GUILayout.Button ("Distance", _buttonStyle,GUILayout.MinWidth(100.0f))) {
				if (_vesselCmpr == VesselComparer.CompareType.DISTANCE)
					_ascend = !_ascend;
				else
					_ascend = true;
				_vesselCmpr = VesselComparer.CompareType.DISTANCE;
				_prevTime = 0.0d;
			}
			GUILayout.EndHorizontal ();

			_scrollPos = GUILayout.BeginScrollView (_scrollPos, _scrollStyle, GUILayout.MinWidth (420.0f), GUILayout.Height (200.0f));
			Vessel vessel = FlightGlobals.ActiveVessel;
			foreach (Vessel v in _vessels) {
				string desc = "";
				if (v.vesselType == VesselType.Debris  && !_debris) {
					continue;
				}
				if (v.vesselType == VesselType.SpaceObject  && !_asteroids) {
					continue;
				}
				if (v.vesselType == VesselType.SpaceObject  && v.DiscoveryInfo.Level < DiscoveryLevels.Name) {
					continue;
				}
				if (v.vesselType == VesselType.Flag) {
					desc = "Flag:";
				}

				GUILayout.BeginHorizontal ();
				GUILayout.Label (desc+v.RevealName (), _labelStyle,GUILayout.MinWidth(150.0f));

				double distance = 0.0d;
				if (vessel != null) {
					distance = Vector3.Distance (v.transform.position, vessel.transform.position);
					GUILayout.Label (KartographStyle.Instance.GetNumberString (distance) + "m", _labelStyle, GUILayout.MinWidth (100.0f));
				}
				if (GUILayout.Button("Target",_buttonStyle)) {
					FlightGlobals.fetch.SetVesselTarget (v);
				}
				if (GUILayout.Button ("Switch", _buttonStyle)) {
					FlightGlobals.SetActiveVessel (v);
				}

				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Close", _buttonStyle)) {
				ToggleWindow ();
			}
			if (GUILayout.Button ("Untarget", _buttonStyle)) {
				FlightGlobals.fetch.SetVesselTarget (null);
			}
			if (!KartographSettings.Instance.DisableKraken && GUILayout.Button ("Unleash the Kraken", _buttonStyle)) {
				if (_krakenWarn) {
					_krakenSacrifice = FlightGlobals.ActiveVessel;
				} else {
					ScreenMessages.PostScreenMessage ("Unleashing the Kraken will destroy the active vessel. You get only one warning.");
					_krakenWarn = true;
				}
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
			_windowStyle 	= KartographStyle.Instance.Window;
			_labelStyle 	= KartographStyle.Instance.Label;
			_centeredLabelStyle = KartographStyle.Instance.CenteredLabel;
			_buttonStyle 	= KartographStyle.Instance.Button;
			_scrollStyle 	= KartographStyle.Instance.ScrollView;
			_toggleStyle 	= KartographStyle.Instance.Toggle;
		}
	}
}

