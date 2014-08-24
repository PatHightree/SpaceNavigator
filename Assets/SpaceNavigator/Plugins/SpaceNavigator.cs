using System;
using System.Collections.Generic;
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
	public static bool LockTranslationX {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationX && !Application.isPlaying; }
		set { _lockTranslationX = value; }
	}
	public static bool LockTranslationY {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationY && !Application.isPlaying; }
		set { _lockTranslationY = value; }
	}
	public static bool LockTranslationZ {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationZ && !Application.isPlaying; }
		set { _lockTranslationZ = value; }
	}
	public static bool LockTranslationAll {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockTranslationAll && !Application.isPlaying; }
		set { _lockTranslationAll = value; }
	}
	public static bool LockRotationX {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationX && !Application.isPlaying; }
		set { _lockRotationX = value; }
	}
	public static bool LockRotationY {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationY && !Application.isPlaying; }
		set { _lockRotationY = value; }
	}
	public static bool LockRotationZ {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationZ && !Application.isPlaying; }
		set { _lockRotationZ = value; }
	}
	public static bool LockRotationAll {
		// At runtime we don't want to limit the output of the driver, its up to the game scripts to do that.
		get { return _lockRotationAll && !Application.isPlaying; }
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
	private int Gears = 3;
	public int CurrentGear = 1;
	public const float TransSensScale = 0.001f, RotSensScale = 0.0015f;

	public const float TransSensDefault = 1f, TransSensMinDefault = 0.1f, TransSensMaxDefault = 10f;
	public float PlayTransSens = TransSensDefault;
	public List<float> TransSens = new List<float> { 0.05f, 1, 50 };
	public List<float> TransSensMin = new List<float>() { 0, 0, 0 };
	public List<float> TransSensMax = new List<float>() { 1, 10, 100 };

	public const float RotSensDefault = 1, RotSensMinDefault = 0, RotSensMaxDefault = 5f;
	public float PlayRotSens = RotSensDefault;
	public float RotSens = RotSensDefault;
	public float RotSensMin =  RotSensMinDefault;
	public float RotSensMax =  RotSensMaxDefault;

	// Setting storage keys
	private const string TransSensKey = "Translation sensitivity";
	private const string TransSensMinKey = "Translation sensitivity minimum";
	private const string TransSensMaxKey = "Translation sensitivity maximum";
	private const string LockTranslationAllKey = "Translation lock all";
	private const string LockTranslationXKey = "Translation lock X";
	private const string LockTranslationYKey = "Translation lock Y";
	private const string LockTranslationZKey = "Translation lock Z";

	private const string RotSensKey = "Rotation sensitivity";
	private const string RotSensMinKey = "Rotation sensitivity minimum";
	private const string RotSensMaxKey = "Rotation sensitivity maximum";
	private const string LockRotationAllKey = "Rotation lock all";
	private const string LockRotationXKey = "Rotation lock X";
	private const string LockRotationYKey = "Rotation lock Y";
	private const string LockRotationZKey = "Rotation lock Z";
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
		#region - Locking -
		#region - Translation -
		GUILayout.BeginHorizontal();
		_lockTranslationAll = GUILayout.Toggle(_lockTranslationAll, "Translation\t");
		GUI.enabled = !_lockTranslationAll;
		_lockTranslationX = GUILayout.Toggle(_lockTranslationX, "X");
		_lockTranslationY = GUILayout.Toggle(_lockTranslationY, "Y");
		_lockTranslationZ = GUILayout.Toggle(_lockTranslationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		#endregion - Translation -

		#region - Rotation -
		GUILayout.BeginHorizontal();
		_lockRotationAll = GUILayout.Toggle(_lockRotationAll, "Rotation\t\t");
		GUI.enabled = !_lockRotationAll;
		_lockRotationX = GUILayout.Toggle(_lockRotationX, "X");
		_lockRotationY = GUILayout.Toggle(_lockRotationY, "Y");
		_lockRotationZ = GUILayout.Toggle(_lockRotationZ, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		#endregion - Rotation -
		#endregion - Locking -
		GUILayout.Space(10);

		#region - Sensitivity + gearbox -
		GUILayout.BeginHorizontal();

		#region - Sensitivity -
		GUILayout.BeginVertical();
		GUILayout.Label("Sensitivity");
		GUILayout.Space(4);


		#region - Translation + rotation -
		GUILayout.BeginVertical();
		#region - Translation -
		GUILayout.BeginHorizontal();
		GUILayout.Label("Translation", GUILayout.Width(67));
		TransSens[CurrentGear] = EditorGUILayout.FloatField(TransSens[CurrentGear], GUILayout.Width(30));
		TransSensMin[CurrentGear] = EditorGUILayout.FloatField(TransSensMin[CurrentGear], GUILayout.Width(30));
		TransSens[CurrentGear] = GUILayout.HorizontalSlider(TransSens[CurrentGear], TransSensMin[CurrentGear], TransSensMax[CurrentGear]);
		TransSensMax[CurrentGear] = EditorGUILayout.FloatField(TransSensMax[CurrentGear], GUILayout.Width(30));
		GUILayout.EndHorizontal();
		#endregion - Translation -

		#region - Rotation -
		GUILayout.BeginHorizontal();
		GUILayout.Label("Rotation", GUILayout.Width(67));
		RotSens = EditorGUILayout.FloatField(RotSens, GUILayout.Width(30));
		RotSensMin = EditorGUILayout.FloatField(RotSensMin, GUILayout.Width(30));
		RotSens = GUILayout.HorizontalSlider(RotSens, RotSensMin, RotSensMax);
		RotSensMax = EditorGUILayout.FloatField(RotSensMax, GUILayout.Width(30));
		GUILayout.EndHorizontal();
		#endregion - Rotation -
		GUILayout.EndVertical();
		#endregion - Translation + rotation -

		GUILayout.EndVertical();
		#endregion - Sensitivity -

		#region - Gearbox -
		GUILayout.BeginVertical();
		GUILayout.Label("Scale", GUILayout.Width(65));
		GUIContent[] modes = new GUIContent[] {
			new GUIContent("Huge", "Galactic scale"),
			new GUIContent("Human", "What people consider 'normal'"),
			new GUIContent("Minuscule", "Itsy-bitsy-scale")
		};
		CurrentGear = GUILayout.SelectionGrid(CurrentGear, modes, 1, GUILayout.Width(67));
		GUILayout.EndVertical();
		#endregion - Gearbox -

		GUILayout.EndHorizontal();
		#endregion - Sensitivity + gearbox -
#endif
	}

	#region - Settings -
	/// <summary>
	/// Reads the settings.
	/// </summary>
	public void ReadSettings() {
		for (int gear = 0; gear < Gears; gear++) {
			TransSens[gear] = PlayerPrefs.GetFloat(TransSensKey + gear, TransSensDefault);
			TransSensMin[gear] = PlayerPrefs.GetFloat(TransSensMinKey + gear, TransSensMinDefault);
			TransSensMax[gear] = PlayerPrefs.GetFloat(TransSensMaxKey + gear, TransSensMaxDefault);
		}
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
		for (int gear = 0; gear < Gears; gear++) {
			PlayerPrefs.SetFloat(TransSensKey + gear, TransSens[gear]);
			PlayerPrefs.SetFloat(TransSensMinKey + gear, TransSensMin[gear]);
			PlayerPrefs.SetFloat(TransSensMaxKey + gear, TransSensMax[gear]);
		}
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
