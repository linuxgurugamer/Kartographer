/*
 * Copyright 2015 SatNet 
 * This file is subject to the included LICENSE.md file. 
 */

using System;
using UnityEngine;
using KSP.IO;

namespace Kartographer
{
	[KSPAddon(KSPAddon.Startup.Instantly,true)]
	public class KartographStyle: MonoBehaviour
	{
		static public KartographStyle Instance
		{
			get { return _instance; }
		}
		static private KartographStyle _instance = null;
		private bool _hasInitStyles = false;
		private GUIStyle 	_windowStyle;
		public GUIStyle Window
		{
			get { return _windowStyle; }
		}
		private GUIStyle 	_labelStyle;
		public GUIStyle Label
		{
			get { return _labelStyle; }
		}
		private GUIStyle 	_centeredLabelStyle;
		public GUIStyle CenteredLabel
		{
			get { return _centeredLabelStyle; }
		}
		private GUIStyle	_buttonStyle;
		public GUIStyle Button
		{
			get { return _buttonStyle; }
		}
		private GUIStyle	_scrollStyle;
		public GUIStyle ScrollView
		{
			get { return _scrollStyle; }
		}
		private GUIStyle	_textFieldStyle;
		public GUIStyle TextField
		{
			get { return _textFieldStyle; }
		}
		private GUIStyle	_textAreaStyle;
		public GUIStyle TextArea
		{
			get { return _textAreaStyle; }
		}
		private GUIStyle _rightLabelStyle;
		public GUIStyle RightLabel
		{
			get { return _rightLabelStyle; }
		}
		private GUIStyle _toggleStyle;
		public GUIStyle Toggle
		{
			get { return _toggleStyle; }
		}
		private int _theme = 0;
		public int Theme
		{
			get { return _theme; }
		}
		public void Start()
		{
			_instance = this;
			DontDestroyOnLoad (this);
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographSettings> ();
			config.load ();
			_theme = config.GetValue<int> ("Theme", 0);
		}

		public void SetTheme(int theme)
		{
			_theme = theme;
			_hasInitStyles = false;
			InitStyle ();
			SaveSettings ();
		}

		/// <summary>
		/// Called when destroying this instance.
		/// </summary>
		public void OnDestroy()
		{
			if (_instance == this)
				_instance = null;
			SaveSettings ();
		}
		public void SaveSettings()
		{
			PluginConfiguration config = PluginConfiguration.CreateForType<KartographStyle> ();
			config.load ();
			config.SetValue ("Theme", _theme);
			config.save ();
		}

		public void OnGUI()
		{
			InitStyle ();
		}

		/// <summary>
		/// Takes a number and formats it for display. Uses standard metric prefixes or scientific notation.
		/// </summary>
		/// <returns>The number string.</returns>
		/// <param name="value">Value.</param>
		public string GetNumberString(double value)
		{
			string unit = " ";
			if (value > 1e12d || (value < 1e-2d && value > 0.0d)) {
				return value.ToString ("e6") + unit;
			}
			else if (value > 1e7d) {
				unit = " M";
				value /= 1e6d;
			}
			else if (value > 1e4d) {
				unit = " k";
				value /= 1e3d;
			}
			string result = value.ToString ("N2") + unit;
			return result;
		}
		/// <summary>
		/// Gets the time string.
		/// </summary>
		/// <returns>The time string.</returns>
		/// <param name="value">Value.</param>
		public string GetUTTimeString(double value)
		{
			return GetTimeString (value + Util.ONE_KYEAR + Util.ONE_KDAY);
		}
		/// <summary>
		/// Gets the time string.
		/// </summary>
		/// <returns>The time string.</returns>
		/// <param name="value">Value.</param>
		public string GetTimeString(double value)
		{
			string result = "";
			if (value < 0.0) {
				result += "-";
				value = Math.Abs (value);
			}
			if (value > Util.ONE_KYEAR) {
				int years = (int)value / (int)Util.ONE_KYEAR;
				value -= (double)(years * Util.ONE_KYEAR);
				result += years + " y,";
			}
			if (value > Util.ONE_KDAY) {
				int days = (int)value / ((int)Util.ONE_KDAY);
				value -= (double)(days * Util.ONE_KDAY);
				result += days + " d,";
			}
			if (value > Util.ONE_KHOUR) {
				int hours = (int)value / ((int)Util.ONE_KHOUR);
				value -= (double)(hours * Util.ONE_KHOUR);
				result += hours + " h,";
			}

			if (value > Util.ONE_KMIN) {
				int mins = (int)value / 60;
				value -= (double)(mins * 60);
				result += mins + " m,";
			}
			result += value.ToString ("N2") + " s";
			return result;
		}

		/// <summary>
		/// Initialize the style elements. Copied from the standard KSP skin with a few modifications.
		/// </summary>
		public void InitStyle()
		{
			if (_hasInitStyles)
				return;

			// Copy the styles so we can change them.
			GUISkin skin = HighLogic.Skin;
			if (_theme != 0) {
				GUISkin temp = GUI.skin;
				GUI.skin = null;
				skin = GUI.skin;
				GUI.skin = temp;
			}

			_windowStyle = new GUIStyle (skin.window);
			_labelStyle = new GUIStyle (skin.label);
			_labelStyle.stretchWidth = true;
			_labelStyle.alignment = TextAnchor.MiddleLeft;
			_centeredLabelStyle = new GUIStyle (skin.label);
			_centeredLabelStyle.stretchWidth = true;
			_centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
			_rightLabelStyle = new GUIStyle (skin.label);
			_rightLabelStyle.stretchWidth = true;
			_rightLabelStyle.alignment = TextAnchor.MiddleRight;
			_buttonStyle = new GUIStyle (skin.button);
			_scrollStyle = new GUIStyle (skin.scrollView);
			_textFieldStyle = new GUIStyle (skin.textField);
			_textAreaStyle = new GUIStyle (skin.textArea);
			_toggleStyle = new GUIStyle (skin.toggle);
			_hasInitStyles = true;

			if (KartographSettings.Instance != null)
				KartographSettings.Instance.InitStyles ();

			if (FocusSelect.Instance != null)
				FocusSelect.Instance.InitStyles ();

			if (AppLauncher.Instance != null)
				AppLauncher.Instance.InitStyles ();

			if (ManeuverEditor.Instance != null)
				ManeuverEditor.Instance.InitStyles ();

			if (WarpTo.Instance != null)
				WarpTo.Instance.InitStyles ();

			if (VesselSelect.Instance != null)
				VesselSelect.Instance.InitStyles ();
		}
	}

}

