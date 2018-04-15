/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ClickThroughFix;
using ToolbarControl_NS;

namespace Kartographer
{
	public delegate void ButtonClickHandler ();

	[KSPAddonImproved (KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class AppLauncher : MonoBehaviour
	{

		const float AUTO_HIDE_TIME = 5.0f;

		static public AppLauncher Instance {
			get { return _instance; }
		}
		static AppLauncher _instance;
		bool _active;
		bool _hidden;
		Rect _windowPos;
		int _winID;
		float _autoHideTime;
        //ApplicationLauncherButton _toolbarButton;
        //IButton _altToolbarButton;
        ToolbarControl toolbarControl;

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

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_windowPos = config.GetValue ("AppLaunchPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));

			GameEvents.onGUIApplicationLauncherReady.Add (OnAppLaunchReady);
			GameEvents.onHideUI.Add (Hide);
			GameEvents.onShowUI.Add (UnHide);
			GameEvents.onGamePause.Add (Hide);
			GameEvents.onGameUnpause.Add (UnHide);
		}

		/// <summary>
		/// Called when this object is destroyed.
		/// </summary>
		public void OnDestroy ()
		{
			ControlUnlock ();

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("AppLaunchPos", _windowPos);
			config.save ();

			DestroyButtons ();

			GameEvents.onGUIApplicationLauncherReady.Remove (OnAppLaunchReady);
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
				_windowPos = ClickThruBlocker.GUILayoutWindow (_winID, _windowPos, OnWindow, "Kartograher");
				if (_windowPos.Contains (Event.current.mousePosition)) {
					ControlLock ();
				} else {
					ControlUnlock ();
				}
			}
		}
        internal const string MODID = "Kartographer_NS";
        internal const string MODNAME = "Kartographer";
        /// <summary>
        /// Callback when the app launcher bar is ready.
        /// </summary>
        internal void OnAppLaunchReady ()
		{
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(ToggleWindow, ToggleWindow,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW |
                    ApplicationLauncher.AppScenes.TRACKSTATION,
                MODID,
                "kartographerButton",
                "Kartographer/PluginData/Textures/kartographer-icon-38",
                "Kartographer/PluginData/Textures/kartographer-icon-24",
                MODNAME
            );
        }

		/// <summary>
		/// Destroy the buttons.
		/// </summary>
		internal void DestroyButtons ()
		{
            toolbarControl.OnDestroy();
            Destroy(toolbarControl);
        }

		/// <summary>
		/// No op.
		/// </summary>
		public void noOp () { }

		/// <summary>
		/// Toggles the window.
		/// </summary>
		internal void ToggleWindow ()
		{
			_active = !_active;
			if (!_active) {
				_autoHideTime = 0.0f;
				ControlUnlock ();
			} else {
				_windowPos.width = 0.0f;
				_windowPos.height = 0.0f;
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

		public void Update ()
		{
			if (KartographSettings.Instance.AutoHide && _autoHideTime != 0.0f && Time.time > _autoHideTime &&
				_active && !_windowPos.Contains (Event.current.mousePosition)) {

                toolbarControl.SetFalse(true);
			}
		}

		void CreateLauncherButton (string text, ButtonClickHandler handler)
		{
			if (GUILayout.Button (text)) {
				handler ();
				_autoHideTime = Time.time + AUTO_HIDE_TIME;
			}
		}

		void CreateLaunchers ()
		{
			if (VesselSelect.Instance != null && VesselSelect.Instance.IsUsable ()) {
				CreateLauncherButton ("Vessel Select", () => {
					VesselSelect.Instance.ToggleWindow ();
				});
			}
			if (CelestialBodyData.Instance != null) {
				CreateLauncherButton ("Celestials Data", () => {
					CelestialBodyData.Instance.ToggleWindow ();
				});
			}
			if (ManeuverEditor.Instance != null && ManeuverEditor.Instance.IsUsable ()) {
				CreateLauncherButton ("Maneuver Editor", () => {
					ManeuverEditor.Instance.ToggleWindow ();
				});
			}
			if (WarpTo.Instance != null) {
				CreateLauncherButton ("Warp To", () => {
					WarpTo.Instance.ToggleWindow ();
				});
			}
			if (KartographSettings.Instance != null) {
				CreateLauncherButton ("Settings", () => {
					KartographSettings.Instance.ToggleWindow ();
				});
			}
		}

		/// <summary>
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void OnWindow (int windowId)
		{
			GUILayout.BeginVertical (GUILayout.Width (150.0f));
			CreateLaunchers ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
