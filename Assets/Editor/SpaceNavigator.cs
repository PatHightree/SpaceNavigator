//#define USE_FAKE_INPUT

using System;
using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEditor;
using UnityEngine;

public class SpaceNavigator : IDisposable {
	// Device variables
	protected Sensor Sensor;
	protected Device Device;
	protected Keyboard Keyboard;

	// Settings
	public float TranslationSensitivity;
	public float RotationSensitivity;

	// Setting defaults
	public const float TranslationSensitivityScale = 0.001f, RotationSensitivityScale = 0.05f;
	public const float TranslationSensitivityDefault = 1f, RotationSensitivityDefault = 1;

	// Setting storage keys
	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";

	public static Vector3 TranslationInWorldSpace {
		get {
#if USE_FAKE_INPUT
			return Instance._fakeTranslationInput;
#else
			return (Instance.Sensor == null ?
				Vector3.zero :
				new Vector3(
					(float)Instance.Sensor.Translation.X,
					(float)Instance.Sensor.Translation.Y,
					-(float)Instance.Sensor.Translation.Z) *
					Instance.TranslationSensitivity * TranslationSensitivityScale);
#endif
		}
	}
	public static Quaternion RotationInWorldSpace {
		get {
#if USE_FAKE_INPUT
			return Quaternion.Euler(Instance._fakeRotationInput.y, Instance._fakeRotationInput.x, 0);
#else
			return (Instance.Sensor == null ?
				Quaternion.identity :
				Quaternion.AngleAxis(
					(float)Instance.Sensor.Rotation.Angle * Instance.RotationSensitivity * RotationSensitivityScale,
					new Vector3(
						-(float)Instance.Sensor.Rotation.X,
						-(float)Instance.Sensor.Rotation.Y,
						(float)Instance.Sensor.Rotation.Z)));
#endif
		}
	}
	public static Quaternion RotationInLocalCoordSys(Transform coordSys) {
		return coordSys.rotation * RotationInWorldSpace * Quaternion.Inverse(coordSys.rotation);
	}

#if USE_FAKE_INPUT
	// For development without SpaceNavigator.
	private Vector2 _fakeRotationInput;
	private Vector3 _fakeTranslationInput;
	private const float FakeInputThreshold = 0.1f;
#endif

	#region - Singleton stuff -
	/// <summary>
	/// Private constructor, prevents a default instance of the <see cref="SpaceNavigator" /> class from being created.
	/// </summary>
	private SpaceNavigator() {
		try {
			if (Device == null) {
				Device = new DeviceClass();
				Sensor = Device.Sensor;
				Keyboard = Device.Keyboard;
				Device.LoadPreferences("Unity");
			}
			if (!Device.IsConnected)
				Device.Connect();
		}
		catch (COMException ex) {
			D.error(ex.ToString());
		}
		D.log("Initialized");
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
			if (Device != null && Device.IsConnected) {
				Device.Disconnect();
				_instance = null;
				GC.Collect();
				D.log("Disconnected");
			}
		}
		catch (COMException ex) {
			D.error(ex.ToString());
		}
	}
	#endregion - IDisposable -

	public void OnGUI() {
#if USE_FAKE_INPUT
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation x {0:0.00000}", _fakeRotationInput.x));
		_fakeRotationInput.x = GUILayout.HorizontalSlider(_fakeRotationInput.x, -1, 1);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation y {0:0.00000}", _fakeRotationInput.y));
		_fakeRotationInput.y = GUILayout.HorizontalSlider(_fakeRotationInput.y, -1, 1);
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
