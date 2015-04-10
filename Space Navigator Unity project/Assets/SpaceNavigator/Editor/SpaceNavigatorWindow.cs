#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SpaceNavigatorWindow : EditorWindow {

	private Vector2 _scrollPos;

	/// <summary>
	/// Initializes the window.
	/// </summary>
	[MenuItem("Window/SpaceNavigator &s")]
	public static void Init() {
		SpaceNavigatorWindow window = GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
		}
	}
	
	public void OnDisable() {
		// Write settings to PlayerPrefs when EditorWindow is closed.
		ViewportController.WriteSettings();
	}
	
	// This does not get called, unfortunately...
	public void OnApplicationQuit() {
		ViewportController.OnApplicationQuit();
	}
	
	public void OnSelectionChange() {
		ViewportController.StoreSelectionTransforms();
	}
	
	public void OnGUI() {
		_scrollPos = GUILayout.BeginScrollView(_scrollPos);
		GUILayout.BeginVertical();

		#region - Operation mode -
		GUILayout.Label("Operation mode");
		GUIContent[] modes = new [] {
				new GUIContent("Fly", "Where do you want to fly today?"),
				new GUIContent("Orbit", "Round, round, round we go"),
				new GUIContent("Telekinesis", "Watch where you're levitating that piano!"),
				new GUIContent("Grab Move", "Excuse me, yes. HDS coming through. I've got a package people")
			};
		ViewportController.Mode = (OperationMode)GUILayout.SelectionGrid((int)ViewportController.Mode, modes, 4);
		#endregion - Operation mode -

		#region - Coordinate system -
		// Enable the coordsys only in Telekinesis mode.
		GUI.enabled = ViewportController.Mode == OperationMode.Telekinesis;
		GUILayout.Label("Coordinate system");
		string[] coordSystems = new [] { "Camera", "World", "Parent", "Local" };
		ViewportController.CoordSys = (CoordinateSystem)GUILayout.SelectionGrid((int)ViewportController.CoordSys, coordSystems, 4);
		#endregion - Coordinate system -

		#region - Snapping -
		// Disable the constraint controls in Fly and Orbit mode.
		GUI.enabled = ViewportController.Mode != OperationMode.Fly && ViewportController.Mode != OperationMode.Orbit;

		GUILayout.Space(10);
		GUILayout.Label("Snap");
		GUILayout.Space(4);
		GUILayout.BeginHorizontal();
		ViewportController.SnapTranslation = GUILayout.Toggle(ViewportController.SnapTranslation, "Grid snap");
		ViewportController.SnapDistance = EditorGUILayout.FloatField(ViewportController.SnapDistance);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		ViewportController.SnapRotation = GUILayout.Toggle(ViewportController.SnapRotation, "Angle snap");
		ViewportController.SnapAngle = EditorGUILayout.IntField(ViewportController.SnapAngle);
		GUILayout.EndHorizontal();

		// Re-enable gui.
		GUI.enabled = true;
		#endregion- Snapping -

		#region - Locking and sensitivity -
		GUILayout.Space(10);
		GUILayout.Label("Lock");
		GUILayout.Space(4);

		EditorGUI.BeginChangeCheck();
		ViewportController.LockHorizon = GUILayout.Toggle(ViewportController.LockHorizon, "Horizon");
		if (EditorGUI.EndChangeCheck() && ViewportController.LockHorizon)
			ViewportController.StraightenHorizon();

		SpaceNavigator.Instance.OnGUI();
		#endregion - Locking and sensitivity -

		#region - Axes inversion per mode -
		GUILayout.Space(10);
		GUILayout.Label("Invert axes in " + ViewportController.Mode.ToString() + " mode");
		GUILayout.Space(4);

		bool tx, ty, tz, rx, ry, rz;
		switch (ViewportController.Mode) {
			case OperationMode.Fly:
				tx = ViewportController.FlyInvertTranslation.x < 0; ty = ViewportController.FlyInvertTranslation.y < 0; tz = ViewportController.FlyInvertTranslation.z < 0;
				rx = ViewportController.FlyInvertRotation.x < 0; ry = ViewportController.FlyInvertRotation.y < 0; rz = ViewportController.FlyInvertRotation.z < 0;
				break;
			case OperationMode.Orbit:
				tx = ViewportController.OrbitInvertTranslation.x < 0; ty = ViewportController.OrbitInvertTranslation.y < 0; tz = ViewportController.OrbitInvertTranslation.z < 0;
				rx = ViewportController.OrbitInvertRotation.x < 0; ry = ViewportController.OrbitInvertRotation.y < 0; rz = ViewportController.OrbitInvertRotation.z < 0;
				break;
			case OperationMode.Telekinesis:
				tx = ViewportController.TelekinesisInvertTranslation.x < 0; ty = ViewportController.TelekinesisInvertTranslation.y < 0; tz = ViewportController.TelekinesisInvertTranslation.z < 0;
				rx = ViewportController.TelekinesisInvertRotation.x < 0; ry = ViewportController.TelekinesisInvertRotation.y < 0; rz = ViewportController.TelekinesisInvertRotation.z < 0;
				break;
			case OperationMode.GrabMove:
				tx = ViewportController.GrabMoveInvertTranslation.x < 0; ty = ViewportController.GrabMoveInvertTranslation.y < 0; tz = ViewportController.GrabMoveInvertTranslation.z < 0;
				rx = ViewportController.GrabMoveInvertRotation.x < 0; ry = ViewportController.GrabMoveInvertRotation.y < 0; rz = ViewportController.GrabMoveInvertRotation.z < 0;
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
			switch (ViewportController.Mode) {
				case OperationMode.Fly:
					ViewportController.FlyInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					ViewportController.OrbitInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					ViewportController.TelekinesisInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					ViewportController.GrabMoveInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
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
			switch (ViewportController.Mode) {
				case OperationMode.Fly:
					ViewportController.FlyInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					ViewportController.OrbitInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					ViewportController.TelekinesisInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					ViewportController.GrabMoveInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		GUILayout.EndHorizontal();
		#endregion - Axes inversion per mode -

		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}
}
#endif