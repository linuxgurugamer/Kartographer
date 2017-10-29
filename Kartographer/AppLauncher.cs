/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;
using KSP.UI.Screens;


namespace Kartographer
{
	public delegate void ButtonClickHandler();

	[KSPAddonImproved(KSPAddonImproved.Startup.Flight | KSPAddonImproved.Startup.TrackingStation, false)]
	public class AppLauncher: MonoBehaviour
	{

		const float AUTO_HIDE_TIME = 5.0f;

		static public AppLauncher Instance
		{
			get { return _instance; }
		}
		static private AppLauncher _instance = null;
		private bool 		_active = false;
		private Rect 		_windowPos = new Rect();
		private GUIStyle 	_windowStyle;
//		private GUIStyle 	_labelStyle;
//		private GUIStyle 	_centeredLabelStyle;
		private GUIStyle	_buttonStyle;
		private int 		_winID;
		private float 		_autoHideTime = 0.0f;
		public ApplicationLauncherButton _toolbarButton;
		private IButton _altToolbarButton = null;
		private PopupMenuDrawable _popMenu = null;

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

			GameEvents.onGUIApplicationLauncherReady.Add (OnAppLaunchReady);
			GameEvents.onGameSceneSwitchRequested.Add (OnSceneChange);
			GameEvents.OnMapEntered.Add (Resize);
			GameEvents.OnMapExited.Add (Resize);
		}

		/// <summary>
		/// Called when this object is destroyed.
		/// </summary>
		public void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLaunchReady);
			GameEvents.onGameSceneSwitchRequested.Remove (OnSceneChange);
			GameEvents.OnMapEntered.Remove (Resize);
			GameEvents.OnMapExited.Remove (Resize);

			DestroyButtons ();
			ControlUnlock ();
			if (_instance == this)
				_instance = null;
		}

		public void OnGUI() {
			if (_active) {
				_windowPos = GUILayout.Window (_winID, _windowPos, OnWindow, "Kartograher", _windowStyle);
				if ((_windowPos.x == 0.0f && _windowPos.y == 0.0f) || _windowPos.yMax > Screen.height) {
					if (_toolbarButton != null) {
						Vector3 toolPos = Camera.current.WorldToScreenPoint (_toolbarButton.GetAnchor ());
						_windowPos.x = toolPos.x - _windowPos.width * 0.5f;
						_windowPos.y = (Screen.height - toolPos.y);
						if (!ApplicationLauncher.Instance.IsPositionedAtTop) {
							_windowPos.y -= _windowPos.height;
						}
					}
				}
				if (_windowPos.xMax + 5.0f > Screen.width) {
					_windowPos.x -= _windowPos.xMax - Screen.width - 5.0f;
				}
			}
			if (_active && _windowPos.Contains (Event.current.mousePosition)) {
				ControlLock ();
			} else {
				ControlUnlock();
			}
		}

		/// <summary>
		/// Callback when the scene changes.
		/// </summary>
		/// <param name="evt">Evt.</param>
		public void OnSceneChange(GameEvents.FromToAction<GameScenes,GameScenes> evt)
		{
			Resize ();
		}

		/// <summary>
		/// Callback when the app launcher bar is ready.
		/// </summary>
		public void OnAppLaunchReady()
		{
			if (ToolbarManager.ToolbarAvailable) {
				_altToolbarButton = ToolbarManager.Instance.add ("Kartographer", "AppLaunch");
				_altToolbarButton.TexturePath = "Kartographer/Textures/sat_map_small";
				_altToolbarButton.Visible = true;
				_altToolbarButton.OnClick += (ClickEvent e) => {
					if (_altToolbarButton.Drawable == null) {
						_popMenu = new PopupMenuDrawable();
						CreateLaunchers();
						_altToolbarButton.Drawable = _popMenu;
					} else {
						_popMenu.Destroy();
						_popMenu = null;
						_altToolbarButton.Drawable = null;
					}
				};
			} else {
				_toolbarButton = ApplicationLauncher.Instance.AddModApplication (
					ToggleWindow,
					ToggleWindow,
					noOp,
					noOp,
					noOp,
					noOp,
					ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW |
					ApplicationLauncher.AppScenes.TRACKSTATION,
					(Texture)GameDatabase.Instance.GetTexture ("Kartographer/Textures/sat_map", false)
				);
			}
		}

		/// <summary>
		/// Destroy the buttons.
		/// </summary>
		public void DestroyButtons()
		{
			if (_toolbarButton != null) {
				ApplicationLauncher.Instance.RemoveModApplication (_toolbarButton);
			}
			if (_altToolbarButton != null) {
				_altToolbarButton.Destroy ();
				_altToolbarButton = null;
			}
		}

		/// <summary>
		/// No op.
		/// </summary>
		public void noOp() {}

		/// <summary>
		/// Force a window resize.
		/// </summary>
		public void Resize()
		{
			_windowPos.height = 0.0f;
		}

		/// <summary>
		/// Toggles the window.
		/// </summary>
		public void ToggleWindow()
		{
			if (_active)
				onDeactivate ();
			else
				onActivate ();
		}

		/// <summary>
		/// Display the app launcher.
		/// </summary>
		public void onActivate()
		{
//			Debug.Log ("Kartographer App Launch Activate - Pos:"+_windowPos.x +" "+_windowPos.y);
			_windowPos.height = 0.0f;
			_active = true;
		}

		/// <summary>
		/// Hide the launcher.
		/// </summary>
		public void onDeactivate()
		{
//			Debug.Log ("Kartographer App Launch Deactivate");
			_active = false;
			_autoHideTime = 0.0f;
			ControlUnlock ();
		}


		/// <summary>
		/// Lock the Controls.
		/// </summary>
		private void ControlLock()
		{
			InputLockManager.SetControlLock (ControlTypes.EDITOR_LOCK, "Kartographer_Launch");
		}
		/// <summary>
		/// Unlock the Controls.
		/// </summary>
		private void ControlUnlock()
		{
			InputLockManager.RemoveControlLock("Kartographer_Launch");
		}

		public void Update()
		{
			if (KartographSettings.Instance.AutoHide && _autoHideTime != 0.0f && Time.time > _autoHideTime &&
				_active && !_windowPos.Contains (Event.current.mousePosition)) {
				_toolbarButton.enabled = false;
				onDeactivate ();
			}
		}

		private void CreateLauncherButton (string text, ButtonClickHandler handler)
		{
			if (_popMenu == null) {
				if (GUILayout.Button (text, _buttonStyle)) {
					handler ();
					_autoHideTime = Time.time + AUTO_HIDE_TIME;
				}
			} else {
				IButton option = _popMenu.AddOption (text);
				option.Text = text;
				option.OnClick += (ClickEvent e) => { handler(); };
			}
		}

		private void CreateLaunchers()
		{
			if (KartographSettings.Instance != null) {
				CreateLauncherButton ("Settings", () => {
					KartographSettings.Instance.ToggleWindow ();
				});
			}
			if (WarpTo.Instance != null) {
				CreateLauncherButton ("Warp To", () => {
					WarpTo.Instance.ToggleWindow ();
				});
			}
			if (FocusSelect.Instance != null && FocusSelect.Instance.IsUsable()) {
				CreateLauncherButton ("Focus Select", () => {
					FocusSelect.Instance.ToggleWindow ();
				});
			}
			if (VesselSelect.Instance != null && VesselSelect.Instance.IsUsable()) {
				CreateLauncherButton ("Vessel Select", () => {
					VesselSelect.Instance.ToggleWindow ();
				});
			}
			if (ManeuverEditor.Instance != null && ManeuverEditor.Instance.IsUsable()	) {
				CreateLauncherButton ("Maneuver Editor", () => {
					ManeuverEditor.Instance.ToggleWindow ();
				});
			}
		}
		/// <summary>
		/// Draws the window.
		/// </summary>
		/// <param name="windowId">Window identifier.</param>
		private void OnWindow(int windowId)
		{
			GUILayout.BeginVertical (GUILayout.Width(150.0f));
			CreateLaunchers ();
			GUILayout.EndVertical ();
			GUI.DragWindow ();
		}

		/// <summary>
		/// Initializes the styles.
		/// </summary>
		public void InitStyles()
		{
			_windowStyle = KartographStyle.Instance.Window;
//			_labelStyle = KartographStyle.Instance.Label;
//			_centeredLabelStyle = KartographStyle.Instance.CenteredLabel;
			_buttonStyle = KartographStyle.Instance.Button;
		}
	}
}

