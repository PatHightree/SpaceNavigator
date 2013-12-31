using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class SpaceNavigator : IDisposable {
	// Public runtime API
	public static Vector3 Translation {
		get { return Instance.GetTranslation(); }
	}
	public static Quaternion Rotation {
		get { return Instance.GetRotation(); }
	}
	public static Quaternion RotationInLocalCoordSys(Transform coordSys) {
		return coordSys.rotation * Rotation * Quaternion.Inverse(coordSys.rotation);
	}
	public static void SetTranslationSensitivity(float newPlayTransSens) {
		Instance.PlayTransSens = newPlayTransSens;
	}
	public static void SetRotationSensitivity(float newPlayRotSens) {
		Instance.PlayRotSens = newPlayRotSens;
	}

	/// <summary>
	/// Locking can be disabled by the SpaceNavigatorWindow;
	/// </summary>
	public static bool IsLockingAllowed = true;
	public static bool LockTranslationX {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationX && IsLockingAllowed && !Application.isPlaying; }
		set { _lockTranslationX = value; }
	}
	public static bool LockTranslationY {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationY && IsLockingAllowed && !Application.isPlaying; }
		set { _lockTranslationY = value; }
	}
	public static bool LockTranslationZ {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationZ && IsLockingAllowed && !Application.isPlaying; }
		set { _lockTranslationZ = value; }
	}
	public static bool LockTranslationAll {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationAll && IsLockingAllowed && !Application.isPlaying; }
		set { _lockTranslationAll = value; }
	}
	public static bool LockRotationX {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationX && IsLockingAllowed && !Application.isPlaying; }
		set { _lockRotationX = value; }
	}
	public static bool LockRotationY {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationY && IsLockingAllowed && !Application.isPlaying; }
		set { _lockRotationY = value; }
	}
	public static bool LockRotationZ {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationZ && IsLockingAllowed && !Application.isPlaying; }
		set { _lockRotationZ = value; }
	}
	public static bool LockRotationAll {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationAll && IsLockingAllowed && !Application.isPlaying; }
		set { _lockRotationAll = value; }
	}

	private static bool _lockTranslationX;
	private static bool _lockTranslationY;
	private static bool _lockTranslationZ;
	private static bool _lockTranslationAll;
	private static bool _lockRotationX;
	private static bool _lockRotationY;
	private static bool _lockRotationZ;
	private static bool _lockRotationAll;

	// Abstract members
	public abstract Vector3 GetTranslation();
	public abstract Quaternion GetRotation();

	// Sensitivity settings
	public const float TransSensScale = 0.001f, RotSensScale = 0.05f;
	public const float TransSensDefault = 10f, TransSensMinDefault = 0.001f, TransSensMaxDefault = 50f;
	public const float RotSensDefault = 1, RotSensMinDefault = 0.001f, RotSensMaxDefault = 5f;
	public float TransSens = TransSensDefault, PlayTransSens = TransSensDefault, TransSensMin = TransSensMinDefault, TransSensMax = TransSensMaxDefault;
	public float RotSens = RotSensDefault, PlayRotSens = RotSensDefault, RotSensMin = RotSensMinDefault, RotSensMax = RotSensMaxDefault;

	// Setting storage keys
	private const string TransSensKey = "Translation sensitivity";
	private const string TransSensMinKey = "Translation sensitivity minimum";
	private const string TransSensMaxKey = "Translation sensitivity maximum";
	private const string LockTranslationAllKey = "Translation lock all";
	private const string LockTranslationXKey = "Translation lock X";
	private const string LockTranslationYKey = "Translation lock Y";
	private const string LockTranslationZKey = "Translation lock Z";
	private const string InvertTranslationXKey = "Translation Invert X";
	private const string InvertTranslationYKey = "Translation Invert Y";
	private const string InvertTranslationZKey = "Translation Invert Z";
	
	private const string RotSensKey = "Rotation sensitivity";
	private const string RotSensMinKey = "Rotation sensitivity minimum";
	private const string RotSensMaxKey = "Rotation sensitivity maximum";
	private const string LockRotationAllKey = "Rotation lock all";
	private const string LockRotationXKey = "Rotation lock X";
	private const string LockRotationYKey = "Rotation lock Y";
	private const string LockRotationZKey = "Rotation lock Z";
	private const string InvertRotationXKey = "Rotation Invert X";
	private const string InvertRotationYKey = "Rotation Invert Y";
	private const string InvertRotationZKey = "Rotation Invert Z";
	#region - Singleton -
	public static SpaceNavigator Instance {
		get {
			if (_instance == null) {
				switch (Application.platform) {
					case RuntimePlatform.OSXEditor:
					case RuntimePlatform.OSXPlayer:
						Debug.LogError("Mac version of the SpaceNavigator driver is not yet implemented, sorry");
						_instance = SpaceNavigatorNoDevice.SubInstance;
						break;
					case RuntimePlatform.WindowsEditor:
					case RuntimePlatform.WindowsPlayer:
						_instance = SpaceNavigatorWindows.SubInstance;
						break;
				}
			}

			return _instance;
		}
		set { _instance = value; }
	}
	private static SpaceNavigator _instance;
	#endregion - Singleton -

	#region - IDisposable -
	public abstract void Dispose();
	#endregion - IDisposable -

	public virtual void OnGUI() {
#if UNITY_EDITOR
		GUILayout.BeginHorizontal();
		_lockTranslationAll = GUILayout.Toggle(_lockTranslationAll, "Translation\t");
		GUI.enabled = !_lockTranslationAll;
		_lockTranslationX = GUILayout.Toggle(_lockTranslationX, "X");
		_lockTranslationY = GUILayout.Toggle(_lockTranslationY, "Y");
		_lockTranslationZ = GUILayout.Toggle(_lockTranslationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		_lockRotationAll = GUILayout.Toggle(_lockRotationAll, "Rotation\t\t");
		GUI.enabled = !_lockRotationAll;
		_lockRotationX = GUILayout.Toggle(_lockRotationX, "X");
		_lockRotationY = GUILayout.Toggle(_lockRotationY, "Y");
		_lockRotationZ = GUILayout.Toggle(_lockRotationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();

		GUILayout.Space(10);
		GUILayout.Label("Sensitivity");
		GUILayout.Space(4);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Translation", GUILayout.Width(75));
		TransSens = EditorGUILayout.FloatField(TransSens, GUILayout.Width(25));
		TransSensMin = EditorGUILayout.FloatField(TransSensMin, GUILayout.Width(25));
		TransSens = GUILayout.HorizontalSlider(TransSens, TransSensMin, TransSensMax);
		TransSensMax = EditorGUILayout.FloatField(TransSensMax, GUILayout.Width(25));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Rotation", GUILayout.Width(75));
		RotSens = EditorGUILayout.FloatField(RotSens, GUILayout.Width(25));
		RotSensMin = EditorGUILayout.FloatField(RotSensMin, GUILayout.Width(25));
		RotSens = GUILayout.HorizontalSlider(RotSens, RotSensMin, RotSensMax);
		RotSensMax = EditorGUILayout.FloatField(RotSensMax, GUILayout.Width(25));
		GUILayout.EndHorizontal();
#endif
	}

	#region - Settings -
	/// <summary>
	/// Reads the settings.
	/// </summary>
	public void ReadSettings() {
		TransSens = PlayerPrefs.GetFloat(TransSensKey, TransSensDefault);
		TransSensMin = PlayerPrefs.GetFloat(TransSensMinKey, TransSensMinDefault);
		TransSensMax = PlayerPrefs.GetFloat(TransSensMaxKey, TransSensMaxDefault);
		_lockTranslationAll = PlayerPrefs.GetInt(LockTranslationAllKey, 0) == 1;
		_lockTranslationX = PlayerPrefs.GetInt(LockTranslationXKey, 0) == 1;
		_lockTranslationY = PlayerPrefs.GetInt(LockTranslationYKey, 0) == 1;
		_lockTranslationZ = PlayerPrefs.GetInt(LockTranslationZKey, 0) == 1;

		RotSens = PlayerPrefs.GetFloat(RotSensKey, RotSensDefault);
		RotSensMin = PlayerPrefs.GetFloat(RotSensMinKey, RotSensMinDefault);
		RotSensMax = PlayerPrefs.GetFloat(RotSensMaxKey, RotSensMaxDefault);
		_lockRotationAll = PlayerPrefs.GetInt(LockRotationAllKey, 0) == 1;
		_lockRotationX = PlayerPrefs.GetInt(LockRotationXKey, 0) == 1;
		_lockRotationY = PlayerPrefs.GetInt(LockRotationYKey, 0) == 1;
		_lockRotationZ = PlayerPrefs.GetInt(LockRotationZKey, 0) == 1;
	}
	/// <summary>
	/// Writes the settings.
	/// </summary>
	public void WriteSettings() {
		PlayerPrefs.SetFloat(TransSensKey, TransSens);
		PlayerPrefs.SetFloat(TransSensMinKey, TransSensMin);
		PlayerPrefs.SetFloat(TransSensMaxKey, TransSensMax);
		PlayerPrefs.SetInt(LockTranslationAllKey, _lockTranslationAll ? 1 : 0);
		PlayerPrefs.SetInt(LockTranslationXKey, _lockTranslationX ? 1 : 0);
		PlayerPrefs.SetInt(LockTranslationYKey, _lockTranslationY ? 1 : 0);
		PlayerPrefs.SetInt(LockTranslationZKey, _lockTranslationZ ? 1 : 0);

		PlayerPrefs.SetFloat(RotSensKey, RotSens);
		PlayerPrefs.SetFloat(RotSensMinKey, RotSensMin);
		PlayerPrefs.SetFloat(RotSensMaxKey, RotSensMax);
		PlayerPrefs.SetInt(LockRotationAllKey, _lockRotationAll ? 1 : 0);
		PlayerPrefs.SetInt(LockRotationXKey, _lockRotationX ? 1 : 0);
		PlayerPrefs.SetInt(LockRotationYKey, _lockRotationY ? 1 : 0);
		PlayerPrefs.SetInt(LockRotationZKey, _lockRotationZ ? 1 : 0);
	}
	#endregion - Settings -
}
