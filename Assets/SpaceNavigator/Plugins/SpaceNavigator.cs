//#define USE_FAKE_INPUT

using System;
using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEditor;
using UnityEngine;

public class SpaceNavigator : IDisposable {
	// Public API
	public static Vector3 TranslationInWorldSpace {
		get {
#if USE_FAKE_INPUT
			return new Vector3(
				LockTranslationX || LockTranslationAll ? 0 : Instance._fakeTranslationInput.x,
				LockTranslationY || LockTranslationAll ? 0 : Instance._fakeTranslationInput.y,
				LockTranslationZ || LockTranslationAll ? 0 : Instance._fakeTranslationInput.z);
#else
			return (Instance._sensor == null ?
				Vector3.zero :
				new Vector3(
					LockTranslationX || LockTranslationAll ? 0: (float)Instance._sensor.Translation.X,
					LockTranslationY || LockTranslationAll ? 0 : (float)Instance._sensor.Translation.Y,
					LockTranslationZ || LockTranslationAll ? 0 : -(float)Instance._sensor.Translation.Z) *
					Instance.TranslationSensitivity * TranslationSensitivityScale);
#endif
		}
	}
	public static Quaternion RotationInWorldSpace {
		get {
#if USE_FAKE_INPUT
			return Quaternion.Euler(
				LockRotationX || LockRotationAll ? 0 : Instance._fakeRotationInput.x,
				LockRotationY || LockRotationAll ? 0 : Instance._fakeRotationInput.y,
				LockRotationZ || LockRotationAll ? 0 : Instance._fakeRotationInput.z);
#else
			return (Instance._sensor == null ?
				Quaternion.identity :
				Quaternion.AngleAxis(
					(float)Instance._sensor.Rotation.Angle * Instance.RotationSensitivity * RotationSensitivityScale,
					new Vector3(
						LockRotationX || LockRotationAll ? 0 : -(float)Instance._sensor.Rotation.X,
						LockRotationY || LockRotationAll ? 0 : -(float)Instance._sensor.Rotation.Y,
						LockRotationZ || LockRotationAll ? 0 : (float)Instance._sensor.Rotation.Z)));
#endif
		}
	}
	public static Quaternion RotationInLocalCoordSys(Transform coordSys) {
		return coordSys.rotation * RotationInWorldSpace * Quaternion.Inverse(coordSys.rotation);
	}
	public static bool LockTranslationX, LockTranslationY, LockTranslationZ, LockTranslationAll;
	public static bool LockRotationX, LockRotationY, LockRotationZ, LockRotationAll;

	// Device variables
	private Sensor _sensor;
	private Device _device;
	private Keyboard _keyboard;

	// Settings
	public float TranslationSensitivity;
	public float RotationSensitivity;

	// Setting defaults
	public const float TranslationSensitivityScale = 0.001f, RotationSensitivityScale = 0.05f;
	public const float TranslationSensitivityDefault = 1f, RotationSensitivityDefault = 1;

	// Setting storage keys
	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";

#if USE_FAKE_INPUT
	// For development without SpaceNavigator.
	private Vector3 _fakeRotationInput;
	private Vector3 _fakeTranslationInput;
	private const float FakeInputThreshold = 0.1f;
#endif

	#region - Singleton -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigator" /> class from being created.
	/// </summary>
	private SpaceNavigator() {
		try {
			if (_device == null) {
				_device = new DeviceClass();
				_sensor = _device.Sensor;
				_keyboard = _device.Keyboard;
			}
			if (!_device.IsConnected)
				_device.Connect();
		}
		catch (COMException ex) {
			Debug.LogError(ex.ToString());
		}
	}

	public static SpaceNavigator Instance {
		get { return _instance ?? (_instance = new SpaceNavigator()); }
	}
	private static SpaceNavigator _instance;
	public static bool HasInstance {
		get { return _instance != null; }
	}
	#endregion - Singleton stuff -

	#region - IDisposable -
	public void Dispose() {
		try {
			if (_device != null && _device.IsConnected) {
				_device.Disconnect();
				_instance = null;
				GC.Collect();
			}
		}
		catch (COMException ex) {
			Debug.LogError(ex.ToString());
		}
	}
	#endregion - IDisposable -

	public void OnGUI() {
		GUILayout.Space(10);
		GUILayout.Label("Lock");
		GUILayout.Space(4);

		GUILayout.BeginHorizontal();
		LockTranslationAll = GUILayout.Toggle(LockTranslationAll, "Translation\t");
		GUI.enabled = !LockTranslationAll;
		LockTranslationX = GUILayout.Toggle(LockTranslationX, "X");
		LockTranslationY = GUILayout.Toggle(LockTranslationY, "Y");
		LockTranslationZ = GUILayout.Toggle(LockTranslationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		LockRotationAll = GUILayout.Toggle(LockRotationAll, "Rotation\t\t");
		GUI.enabled = !LockRotationAll;
		LockRotationX = GUILayout.Toggle(LockRotationX, "X");
		LockRotationY = GUILayout.Toggle(LockRotationY, "Y");
		LockRotationZ = GUILayout.Toggle(LockRotationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.Space(10);
		GUILayout.Label("Sensitivity");
		GUILayout.Space(4);

		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Translation\t {0:0.00000}", TranslationSensitivity));
		TranslationSensitivity = GUILayout.HorizontalSlider(TranslationSensitivity, 0.001f, 5f);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Rotation\t\t {0:0.00000}", RotationSensitivity));
		RotationSensitivity = GUILayout.HorizontalSlider(RotationSensitivity, 0.001f, 5f);
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Reset")) {
			TranslationSensitivity = 1;
			RotationSensitivity = 1;
		}

#if USE_FAKE_INPUT
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation x {0:0.00000}", _fakeRotationInput.x));
		_fakeRotationInput.x = GUILayout.HorizontalSlider(_fakeRotationInput.x, -1, 1);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation y {0:0.00000}", _fakeRotationInput.y));
		_fakeRotationInput.y = GUILayout.HorizontalSlider(_fakeRotationInput.y, -1, 1);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation z {0:0.00000}", _fakeRotationInput.z));
		_fakeRotationInput.z = GUILayout.HorizontalSlider(_fakeRotationInput.z, -1, 1);
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation x {0:0.00000}", _fakeTranslationInput.x));
		_fakeTranslationInput.x = GUILayout.HorizontalSlider(_fakeTranslationInput.x, -0.05f, 0.05f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation y {0:0.00000}", _fakeTranslationInput.y));
		_fakeTranslationInput.y = GUILayout.HorizontalSlider(_fakeTranslationInput.y, -0.05f, 0.05f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation z {0:0.00000}", _fakeTranslationInput.z));
		_fakeTranslationInput.z = GUILayout.HorizontalSlider(_fakeTranslationInput.z, -0.05f, 0.05f);
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Stop")) {
			_fakeRotationInput = Vector2.zero;
			_fakeTranslationInput = Vector3.zero;
		}

		if (Mathf.Abs(_fakeRotationInput.x) < FakeInputThreshold)
			_fakeRotationInput.x = 0;
		if (Mathf.Abs(_fakeRotationInput.y) < FakeInputThreshold)
			_fakeRotationInput.y = 0;
#endif
	}

	#region - Settings -
	/// <summary>
	/// Reads the settings.
	/// </summary>
	public void ReadSettings() {
		TranslationSensitivity = EditorPrefs.GetFloat(TransSensKey, TranslationSensitivityDefault);
		RotationSensitivity = EditorPrefs.GetFloat(RotSensKey, RotationSensitivityDefault);
	}
	/// <summary>
	/// Writes the settings.
	/// </summary>
	public void WriteSettings() {
		EditorPrefs.SetFloat(TransSensKey, TranslationSensitivity);
		EditorPrefs.SetFloat(RotSensKey, RotationSensitivity);
	}
	#endregion - Settings -
}
