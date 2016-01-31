using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum OperationMode { Fly, Orbit, Telekinesis, GrabMove }
public enum CoordinateSystem { Camera, World, Parent, Local }

[Serializable]
public static class Settings {
	[SerializeField]
	public static OperationMode Mode;
	[SerializeField]
	public static CoordinateSystem CoordSys;

	// Snapping
	public static bool SnapRotation;
	public static int SnapAngle = 45;
	public static bool SnapTranslation;
	public static float SnapDistance = 0.1f;

	// Locking
	public static bool LockHorizon = true;
	[SerializeField]
	public static Locks LockTranslation;
	[SerializeField]
	public static Locks LockRotation;

	// Sensitivity
	private static int Gears = 3;
	public static int CurrentGear = 1;

	public static List<float> TransSensDefault = new List<float> { 50, 1, 0.05f };
	public static List<float> TransSensMinDefault = new List<float>() { 1, 0.1f, 0.01f };
	public static List<float> TransSensMaxDefault = new List<float>() { 100, 10, 1 };
	public static float PlayTransSens = TransSensDefault[1];
	public static List<float> TransSens = new List<float>(TransSensDefault);
	public static List<float> TransSensMin = new List<float>(TransSensMinDefault);
	public static List<float> TransSensMax = new List<float>(TransSensMaxDefault);

	public const float RotSensDefault = 1, RotSensMinDefault = 0, RotSensMaxDefault = 5f;
	public static float PlayRotSens = RotSensDefault;
	public static float RotSens = RotSensDefault;
	public static float RotSensMin = RotSensMinDefault;
	public static float RotSensMax = RotSensMaxDefault;

	// Setting storage keys
	private const string TransSensKey = "Translation sensitivity";
	private const string TransSensMinKey = "Translation sensitivity minimum";
	private const string TransSensMaxKey = "Translation sensitivity maximum";
	private const string RotSensKey = "Rotation sensitivity";
	private const string RotSensMinKey = "Rotation sensitivity minimum";
	private const string RotSensMaxKey = "Rotation sensitivity maximum";

	public static bool RuntimeEditorNav = true;

	// Inversion
	public static Vector3 FlyInvertTranslation, FlyInvertRotation;
	public static Vector3 OrbitInvertTranslation, OrbitInvertRotation;
	public static Vector3 TelekinesisInvertTranslation, TelekinesisInvertRotation;
	public static Vector3 GrabMoveInvertTranslation, GrabMoveInvertRotation;

	private static Vector2 _scrollPos;

	static Settings() {
		//Debug.Log("New Settings()");
		LockTranslation = new Locks("Translation");
		LockRotation = new Locks("Rotation");
	}

	public static void OnGUI() {
#if UNITY_EDITOR
		_scrollPos = GUILayout.BeginScrollView(_scrollPos);
		GUILayout.BeginVertical();

		#region - Operation mode -
		GUILayout.Label("Operation mode");
		GUIContent[] modes = new[] {
				new GUIContent("Fly", "Where do you want to fly today?"),
				new GUIContent("Orbit", "Round, round, round we go"),
				new GUIContent("Telekinesis", "Watch where you're levitating that piano!"),
				new GUIContent("Grab Move", "Excuse me, yes. HDS coming through. I've got a package people")
			};
		Mode = (OperationMode)GUILayout.SelectionGrid((int)Mode, modes, 4);
		#endregion - Operation mode -

		#region - Coordinate system -
		// Enable the coordsys only in Telekinesis mode.
		GUI.enabled = Mode == OperationMode.Telekinesis;
		GUILayout.Label("Coordinate system");
		string[] coordSystems = new [] { "Camera", "World", "Parent", "Local" };
		CoordSys = (CoordinateSystem)GUILayout.SelectionGrid((int)CoordSys, coordSystems, 4);
		#endregion - Coordinate system -

		#region - Snapping -
		// Disable the constraint controls in Fly and Orbit mode.
		GUI.enabled = Settings.Mode != OperationMode.Fly && Settings.Mode != OperationMode.Orbit;

		GUILayout.Space(10);
		GUILayout.Label("Snap");
		GUILayout.Space(4);
		GUILayout.BeginHorizontal();
		SnapTranslation = GUILayout.Toggle(SnapTranslation, "Grid snap");
		SnapDistance = EditorGUILayout.FloatField(SnapDistance);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		SnapRotation = GUILayout.Toggle(SnapRotation, "Angle snap");
		SnapAngle = EditorGUILayout.IntField(SnapAngle);
		GUILayout.EndHorizontal();

		// Re-enable gui.
		GUI.enabled = true;
		#endregion- Snapping -

		#region - Locking -

		GUILayout.Space(10);
		GUILayout.Label("Lock");
		GUILayout.Space(4);
		LockHorizon = GUILayout.Toggle(LockHorizon, "Horizon");

		#region - Translation -
		GUILayout.BeginHorizontal();
		LockTranslation.All = GUILayout.Toggle(LockTranslation.All, "Translation", GUILayout.Width(100));
		GUI.enabled = !LockTranslation.All;
		LockTranslation.X = GUILayout.Toggle(LockTranslation.X, "X");
		LockTranslation.Y = GUILayout.Toggle(LockTranslation.Y, "Y");
		LockTranslation.Z = GUILayout.Toggle(LockTranslation.Z, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		#endregion - Translation -

		#region - Rotation -
		GUILayout.BeginHorizontal();
		LockRotation.All = GUILayout.Toggle(LockRotation.All, "Rotation", GUILayout.Width(100));
		GUI.enabled = !LockRotation.All;
		LockRotation.X = GUILayout.Toggle(LockRotation.X, "X");
		LockRotation.Y = GUILayout.Toggle(LockRotation.Y, "Y");
		LockRotation.Z = GUILayout.Toggle(LockRotation.Z, "Z");
		GUI.enabled = true;
		GUILayout.EndHorizontal();
		#endregion - Rotation -

		#endregion - Locking -

		#region - Sensitivity + gearbox -
		GUILayout.Space(10);
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
		modes = new GUIContent[] {
				new GUIContent("Huge", "Galactic scale"),
				new GUIContent("Human", "What people consider 'normal'"),
				new GUIContent("Minuscule", "Itsy-bitsy-scale")
			};
		CurrentGear = GUILayout.SelectionGrid(CurrentGear, modes, 1, GUILayout.Width(67));
		GUILayout.EndVertical();
		#endregion - Gearbox -

		GUILayout.EndHorizontal();
		#endregion - Sensitivity + gearbox -

		RuntimeEditorNav = GUILayout.Toggle(RuntimeEditorNav, "Runtime Editor Navigation");

		#region - Axes inversion per mode -
		GUILayout.Space(10);
		GUILayout.Label("Invert axes in " + Settings.Mode.ToString() + " mode");
		GUILayout.Space(4);

		bool tx, ty, tz, rx, ry, rz;
		switch (Settings.Mode) {
			case OperationMode.Fly:
				tx = Settings.FlyInvertTranslation.x < 0; ty = Settings.FlyInvertTranslation.y < 0; tz = Settings.FlyInvertTranslation.z < 0;
				rx = Settings.FlyInvertRotation.x < 0; ry = Settings.FlyInvertRotation.y < 0; rz = Settings.FlyInvertRotation.z < 0;
				break;
			case OperationMode.Orbit:
				tx = Settings.OrbitInvertTranslation.x < 0; ty = Settings.OrbitInvertTranslation.y < 0; tz = Settings.OrbitInvertTranslation.z < 0;
				rx = Settings.OrbitInvertRotation.x < 0; ry = Settings.OrbitInvertRotation.y < 0; rz = Settings.OrbitInvertRotation.z < 0;
				break;
			case OperationMode.Telekinesis:
				tx = Settings.TelekinesisInvertTranslation.x < 0; ty = Settings.TelekinesisInvertTranslation.y < 0; tz = Settings.TelekinesisInvertTranslation.z < 0;
				rx = Settings.TelekinesisInvertRotation.x < 0; ry = Settings.TelekinesisInvertRotation.y < 0; rz = Settings.TelekinesisInvertRotation.z < 0;
				break;
			case OperationMode.GrabMove:
				tx = Settings.GrabMoveInvertTranslation.x < 0; ty = Settings.GrabMoveInvertTranslation.y < 0; tz = Settings.GrabMoveInvertTranslation.z < 0;
				rx = Settings.GrabMoveInvertRotation.x < 0; ry = Settings.GrabMoveInvertRotation.y < 0; rz = Settings.GrabMoveInvertRotation.z < 0;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label("Translation", GUILayout.Width(100));
		EditorGUI.BeginChangeCheck();
		tx = GUILayout.Toggle(tx, "X");
		ty = GUILayout.Toggle(ty, "Y");
		tz = GUILayout.Toggle(tz, "Z");
		if (EditorGUI.EndChangeCheck()) {
			switch (Settings.Mode) {
				case OperationMode.Fly:
					Settings.FlyInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					Settings.OrbitInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					Settings.TelekinesisInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					Settings.GrabMoveInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Rotation", GUILayout.Width(100));
		EditorGUI.BeginChangeCheck();

		rx = GUILayout.Toggle(rx, "X");
		ry = GUILayout.Toggle(ry, "Y");
		rz = GUILayout.Toggle(rz, "Z");
		if (EditorGUI.EndChangeCheck()) {
			switch (Settings.Mode) {
				case OperationMode.Fly:
					Settings.FlyInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					Settings.OrbitInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					Settings.TelekinesisInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					Settings.GrabMoveInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		GUILayout.EndHorizontal();
		#endregion - Axes inversion per mode -

		GUILayout.EndVertical();
		GUILayout.EndScrollView();
#endif
	}

	public static void Write() {
		//Debug.Log("Write settings");

		// Navigation Mode
		PlayerPrefs.SetInt("Navigation mode", (int)Mode);
		// Coordinate System
		PlayerPrefs.SetInt("Coordinate System", (int)CoordSys);
		// Snap
		PlayerPrefs.SetInt("Snap Translation", SnapTranslation ? 1 : 0);
		PlayerPrefs.SetFloat("Snap Distance", SnapDistance);
		PlayerPrefs.SetInt("Snap Rotation", SnapRotation ? 1 : 0);
		PlayerPrefs.SetInt("Snap Angle", SnapAngle);
		// Lock Horizon
		PlayerPrefs.SetInt("LockHorizon", LockHorizon ? 1 : 0);
		// Lock Axis
		LockTranslation.Write();
		LockRotation.Write();
		// Sensitivity
		for (int gear = 0; gear < Gears; gear++) {
			PlayerPrefs.SetFloat(TransSensKey + gear, TransSens[gear]);
			PlayerPrefs.SetFloat(TransSensMinKey + gear, TransSensMin[gear]);
			PlayerPrefs.SetFloat(TransSensMaxKey + gear, TransSensMax[gear]);
		}
		PlayerPrefs.SetFloat(RotSensKey, RotSens);
		PlayerPrefs.SetFloat(RotSensMinKey, RotSensMin);
		PlayerPrefs.SetFloat(RotSensMaxKey, RotSensMax);
		// Runtime Editor Navigation
		PlayerPrefs.SetInt("RuntimeEditorNav", RuntimeEditorNav ? 1 : 0);
		// Axis Inversions
		WriteAxisInversions(FlyInvertTranslation, FlyInvertRotation, "Fly");
		WriteAxisInversions(OrbitInvertTranslation, OrbitInvertRotation, "Orbit");
		WriteAxisInversions(TelekinesisInvertTranslation, TelekinesisInvertRotation, "Telekinesis");
		WriteAxisInversions(GrabMoveInvertTranslation, GrabMoveInvertRotation, "Grab move");
	}

	public static void Read() {
		//Debug.Log("Read settings");

		// Navigation Mode
		Mode = (OperationMode)PlayerPrefs.GetInt("Navigation mode", (int)OperationMode.Fly);
		// Coordinate System
		CoordSys = (CoordinateSystem)PlayerPrefs.GetInt("Coordinate System", (int)CoordinateSystem.Camera);
		// Snap
		SnapTranslation = PlayerPrefs.GetInt("Snap Translation", 0) == 1;
		SnapDistance = PlayerPrefs.GetFloat("Snap Distance", 0.1f);
		SnapRotation = PlayerPrefs.GetInt("Snap Rotation", 0) == 1;
		SnapAngle = PlayerPrefs.GetInt("Snap Angle", 45);
		// Lock Horizon
		LockHorizon = PlayerPrefs.GetInt("LockHorizon", 1) == 1;
		// Lock Axis
		LockTranslation.Read();
		LockRotation.Read();
		// Sensitivity
		for (int gear = 0; gear < Gears; gear++) {
			TransSens[gear] = PlayerPrefs.GetFloat(TransSensKey + gear, TransSensDefault[gear]);
			TransSensMin[gear] = PlayerPrefs.GetFloat(TransSensMinKey + gear, TransSensMinDefault[gear]);
			TransSensMax[gear] = PlayerPrefs.GetFloat(TransSensMaxKey + gear, TransSensMaxDefault[gear]);
		}
		RotSens = PlayerPrefs.GetFloat(RotSensKey, RotSensDefault);
		RotSensMin = PlayerPrefs.GetFloat(RotSensMinKey, RotSensMinDefault);
		RotSensMax = PlayerPrefs.GetFloat(RotSensMaxKey, RotSensMaxDefault);
		// Runtime Editor Navigation
		RuntimeEditorNav = PlayerPrefs.GetInt("RuntimeEditorNav", 1) == 1;
		// Axis Inversions
		ReadAxisInversions(ref FlyInvertTranslation, ref FlyInvertRotation, "Fly");
		ReadAxisInversions(ref OrbitInvertTranslation, ref OrbitInvertRotation, "Orbit");
		ReadAxisInversions(ref TelekinesisInvertTranslation, ref TelekinesisInvertRotation, "Telekinesis");
		ReadAxisInversions(ref GrabMoveInvertTranslation, ref GrabMoveInvertRotation, "Grab move");
	}

	private static void WriteAxisInversions(Vector3 translation, Vector3 rotation, string baseName) {
		PlayerPrefs.SetInt(baseName + " invert translation x", translation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation y", translation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation z", translation.z < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation x", rotation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation y", rotation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation z", rotation.z < 0 ? -1 : 1);
	}

	private static void ReadAxisInversions(ref Vector3 translation, ref Vector3 rotation, string baseName) {
		translation.x = PlayerPrefs.GetInt(baseName + " invert translation x", 1);
		translation.y = PlayerPrefs.GetInt(baseName + " invert translation y", 1);
		translation.z = PlayerPrefs.GetInt(baseName + " invert translation z", 1);
		rotation.x = PlayerPrefs.GetInt(baseName + " invert rotation x", 1);
		rotation.y = PlayerPrefs.GetInt(baseName + " invert rotation y", 1);
		rotation.z = PlayerPrefs.GetInt(baseName + " invert rotation z", 1);
	}
}
