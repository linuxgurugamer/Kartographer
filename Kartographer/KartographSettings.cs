/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;
using KSP.IO;

namespace Kartographer
{
	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.EditorAny | KSPAddonImproved.Startup.TrackingStation, false)]
	public class KartographSettings: MonoBehaviour
	{
		static public KartographSettings Instance
		{
			get { return _instance; }
		}
		static private KartographSettings _instance = null;
		private GUIStyle 	_windowStyle;
		private GUIStyle 	_labelStyle;
//		private GUIStyle 	_centeredLabelStyle;
//		private GUIStyle	_textFieldStyle;
//		private GUIStyle	_textAreaStyle;
		private GUIStyle	_toggleStyle;
		private GUIStyle	_buttonStyle;
//		private bool 		_hasInitStyles 	= false;
		private bool		_active = false;
		static private Rect _windowPos = new Rect();
		private bool _autoHide = true;
		public bool AutoHide { get { return _autoHide; } }
		private bool _disableKraken = false;
		public bool DisableKraken { get { return _disableKraken; } }


		private int _winID = 0;

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

			InitStyles ();

			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_autoHide = config.GetValue<bool> ("AutoHide", true);
			_disableKraken = config.GetValue<bool> ("KrakenDisable", false);
		}

		/// <summary>
		/// Called when destroying this instance.
		/// </summary>
		public void OnDestroy()
		{
			if (_instance == this)
				_instance = null;
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			config.SetValue ("AutoHide", _autoHide);
			config.SetValue ("KrakenDisable", _disableKraken);
			config.save ();
		}

		public void OnGUI()
		{
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Settings", _windowStyle);
				if (_windowPos.x == 0.0f && _windowPos.y == 0.0f) {
					_windowPos.y = Screen.height * 0.5f - _windowPos.height * 0.5f;
					_windowPos.x = 50.0f;
				}
			}
		}

		/// <summary>
		/// Toggles the window.
		/// </summary>
		public void ToggleWindow()
		{
			_active = !_active;
		}

		/// <summary>
		/// Draw the main window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical (GUILayout.MinWidth(300.0f));
			GUILayout.Label ("Plugin:" + typeof(KartographSettings).Assembly.GetName ().Name, _labelStyle);
//			GUILayout.Label ("Version:"+ Util.VERSION, _labelStyle);
			GUILayout.Label ("Version: "+ typeof(KartographSettings).Assembly.GetName ().Version, _labelStyle);
			_autoHide = GUILayout.Toggle (_autoHide, "Auto Hide Utilities Launcher",_toggleStyle);
			_disableKraken = GUILayout.Toggle (_disableKraken, "Disable \"Unleash the Kraken\"",_toggleStyle);
			int themeSelection = GUILayout.SelectionGrid (KartographStyle.Instance.Theme, 
				new string[]{ "KSP", "Unity" }, 3, _buttonStyle,
				GUILayout.MinWidth (300.0f));
			if (themeSelection != KartographStyle.Instance.Theme)
				KartographStyle.Instance.SetTheme (themeSelection);

			if (GUILayout.Button ("Close", _buttonStyle)) {
				ToggleWindow ();
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
//			_textFieldStyle = KartographStyle.Instance.TextField;
//			_textAreaStyle = KartographStyle.Instance.TextArea;
			_buttonStyle = KartographStyle.Instance.Button;
			_toggleStyle = KartographStyle.Instance.Toggle;
		}
	}
}

