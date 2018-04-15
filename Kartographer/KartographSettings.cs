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
	public class KartographSettings : MonoBehaviour
	{

		static KartographSettings _instance;
		static internal KartographSettings Instance {
			get { return _instance; }
		}

		bool _active;
		bool _hidden;
		Rect _windowPos;
		int _winID;

		bool _autoHide;
		internal bool AutoHide { get { return _autoHide; } }
		bool _disableKraken;
		internal bool DisableKraken { get { return _disableKraken; } }
		bool _useKspSkin;
		internal bool UseKspSkin { get { return _useKspSkin; } }


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
			_autoHide = config.GetValue ("AutoHide", true);
			_disableKraken = config.GetValue ("KrakenDisable", false);
			_useKspSkin = config.GetValue ("UseKspSkin", true);

			_windowPos = config.GetValue ("SettingsWindowPos", new Rect (new Vector2 (Screen.width / 2, Screen.height / 2), Vector2.zero));

			GameEvents.onHideUI.Add (Hide);
			GameEvents.onShowUI.Add (UnHide);
			GameEvents.onGamePause.Add (Hide);
			GameEvents.onGameUnpause.Add (UnHide);
		}

		/// <summary>
		/// Called when destroying this instance.
		/// </summary>
		public void OnDestroy ()
		{
			ControlUnlock ();
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("AutoHide", _autoHide);
			config.SetValue ("KrakenDisable", _disableKraken);
			config.SetValue ("UseKspSkin", _useKspSkin);

			config.SetValue ("SettingsWindowPos", _windowPos);
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
				if (_useKspSkin) GUI.skin = HighLogic.Skin;
				_windowPos = ClickThruBlocker.GUILayoutWindow (_winID, _windowPos, OnWindow, "Settings");
				if (_windowPos.Contains (Event.current.mousePosition)) {
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
			InputLockManager.SetControlLock (ControlTypes.ALLBUTTARGETING, name);
		}

		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		void ControlUnlock ()
		{
			InputLockManager.RemoveControlLock (name);
		}

		/// <summary>
		/// Draw the main window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		void OnWindow (int windowId)
		{
			GUILayout.BeginVertical (GUILayout.MinWidth (300.0f));
			GUILayout.Label ("Plugin:" + typeof (KartographSettings).Assembly.GetName ().Name);
			//			GUILayout.Label ("Version:"+ Util.VERSION);
			GUILayout.Label ("Version: " + typeof (KartographSettings).Assembly.GetName ().Version);

			_autoHide = GUILayout.Toggle (_autoHide, "Auto Hide Utilities Launcher");
			_disableKraken = GUILayout.Toggle (_disableKraken, "Disable \"Unleash the Kraken\"");
			_useKspSkin = GUILayout.Toggle (_useKspSkin, "Use KSP Skin");

			GUILayout.BeginHorizontal ();

            if (GUILayout.Button ("Close")) {
				ToggleWindow ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}
	}
}
