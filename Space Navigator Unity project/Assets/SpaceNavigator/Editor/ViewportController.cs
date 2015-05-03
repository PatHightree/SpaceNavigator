using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public enum OperationMode { Fly, Orbit, Telekinesis, GrabMove }
public enum CoordinateSystem { Camera, World, Parent, Local }

[InitializeOnLoad]
class ViewportController {
	public static OperationMode Mode;
	public static bool LockHorizon = true;
	public static CoordinateSystem CoordSys;

	// Snapping
	private static Dictionary<Transform, Quaternion> _unsnappedRotations = new Dictionary<Transform, Quaternion>();
	private static Dictionary<Transform, Vector3> _unsnappedTranslations = new Dictionary<Transform, Vector3>();
	public static bool SnapRotation;
	public static int SnapAngle = 45;
	public static bool SnapTranslation;
	public static float SnapDistance = 0.1f;
	private static bool _wasIdle;

	// Rig components
	[SerializeField]
	private static GameObject _pivotGO, _cameraGO;
	[SerializeField]
	private static Transform _pivot, _camera;

	// Inversion
	public static Vector3 FlyInvertTranslation, FlyInvertRotation;
	public static Vector3 OrbitInvertTranslation, OrbitInvertRotation;
	public static Vector3 TelekinesisInvertTranslation, TelekinesisInvertRotation;
	public static Vector3 GrabMoveInvertTranslation, GrabMoveInvertRotation;

	static ViewportController() {
		EditorApplication.update += Update;
		ReadSettings();
		InitCameraRig();
		StoreSelectionTransforms();
	}
	public static void OnApplicationQuit() {
		WriteSettings();
		DisposeCameraRig();
		SpaceNavigator.Instance.Dispose();
	}
	static void Update() {
		// This function should only operate while editing.
		if (Application.isPlaying) return;

		SceneView sceneView = SceneView.lastActiveSceneView;
		if (!sceneView) return;

		// Return if device is idle.
		if (SpaceNavigator.Translation == Vector3.zero &&
			SpaceNavigator.Rotation == Quaternion.identity) {
			_wasIdle = true;
			return;
		}

		switch (Mode) {
			case OperationMode.Fly:
				Fly(sceneView);
				break;
			case OperationMode.Orbit:
				Orbit(sceneView);
				break;
			case OperationMode.Telekinesis:
				// Manipulate the object free from the camera.
				Telekinesis(sceneView);
				break;
			case OperationMode.GrabMove:
				// Manipulate the object together with the camera.
				GrabMove(sceneView);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		//// Detect keyboard clicks (not working).
		//if (Keyboard.IsKeyDown(1))
		//	D.log("Button 0 pressed");
		//if (Keyboard.IsKeyDown(2))
		//	D.log("Button 1 pressed");

		_wasIdle = false;
	}

	#region - Navigation -
	static void Fly(SceneView sceneView) {
		Fly(sceneView, FlyInvertTranslation, FlyInvertRotation);
	}
	static void Fly(SceneView sceneView, Vector3 translationInversion, Vector3 rotationInversion) {
		SyncRigWithScene();

		// Apply inversion of axes for fly/grabmove mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, translationInversion);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, rotationInversion);

		_camera.Translate(translation, Space.Self);
		if (sceneView.orthographic)
			sceneView.size -= translation.z;
		else {
			if (LockHorizon) {
				// Perform azimuth in world coordinates.
				_camera.Rotate(Vector3.up, rotation.y, Space.World);
				// Perform pitch in local coordinates.
				_camera.Rotate(Vector3.right, rotation.x, Space.Self);
			} else {
				// Default rotation method, applies the whole quaternion to the camera.
				_camera.Rotate(rotation);
			}
		}

		// Update sceneview pivot and repaint view.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}
	static void Orbit(SceneView sceneView) {
		// If no object is selected don't orbit, fly instead.
		if (Selection.gameObjects.Length == 0) {
			Fly(sceneView);
			return;
		}

		SyncRigWithScene();

		// Apply inversion of axes for orbit mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, OrbitInvertTranslation);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, OrbitInvertRotation);

		_camera.Translate(translation, Space.Self);

		if (LockHorizon) {
			_camera.RotateAround(Tools.handlePosition, Vector3.up, rotation.y);
			_camera.RotateAround(Tools.handlePosition, _camera.right, rotation.x);
		} else {
			_camera.RotateAround(Tools.handlePosition, _camera.up, rotation.y);
			_camera.RotateAround(Tools.handlePosition, _camera.right, rotation.x);
			_camera.RotateAround(Tools.handlePosition, _camera.forward, rotation.z);
		}

		// Update sceneview pivot and repaint view.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}
	static void Telekinesis(SceneView sceneView) {
		// Apply inversion of axes for telekinesis mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, TelekinesisInvertTranslation);
		Quaternion rotation = Quaternion.Euler(Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, TelekinesisInvertRotation));

		// Store the selection's transforms because the user could have edited them since we last used them via the inspector.
		if (_wasIdle)
			StoreSelectionTransforms();

		foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
			if (!_unsnappedRotations.ContainsKey(transform)) continue;

			Transform reference;
			switch (CoordSys) {
				case CoordinateSystem.Camera:
					reference = sceneView.camera.transform;
					break;
				case CoordinateSystem.World:
					reference = null;
					break;
				case CoordinateSystem.Parent:
					reference = transform.parent;
					break;
				case CoordinateSystem.Local:
					reference = transform;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (reference == null) {
				// Move the object in world coordinates.
				_unsnappedTranslations[transform] += translation;
				_unsnappedRotations[transform] = rotation * _unsnappedRotations[transform];
			} else {
				// Move the object in the reference coordinate system.
				Vector3 worldTranslation = reference.TransformPoint(translation) -
										   reference.position;
				_unsnappedTranslations[transform] += worldTranslation;
				_unsnappedRotations[transform] = (reference.rotation * rotation * Quaternion.Inverse(reference.rotation)) * _unsnappedRotations[transform];
			}

			// Perform rotation with or without snapping.
			transform.rotation = SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], SnapAngle) : _unsnappedRotations[transform];
			transform.position = SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], SnapDistance) : _unsnappedTranslations[transform];
		}
	}
	static void GrabMove(SceneView sceneView) {
		// Apply inversion of axes for grab move mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, GrabMoveInvertTranslation);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, GrabMoveInvertRotation);

		// Store the selection's transforms because the user could have edited them since we last used them via the inspector.
		if (_wasIdle)
			StoreSelectionTransforms();

		foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
			if (!_unsnappedRotations.ContainsKey(transform)) continue;

			// Initialize transform to unsnapped state.
			transform.rotation = _unsnappedRotations[transform];
			transform.position = _unsnappedTranslations[transform];
			Vector3 oldPos = transform.position;

			// Rotate with horizon lock.
			transform.RotateAround(_camera.position, Vector3.up, rotation.y);
			transform.RotateAround(_camera.position, _camera.right, rotation.x);

			// Interpret SpaceNavigator input in camera space, calculate the effect in world space.
			Vector3 worldTranslation = sceneView.camera.transform.TransformPoint(translation) -
										sceneView.camera.transform.position;
			transform.position += worldTranslation;

			// Store new unsnapped state.
			_unsnappedRotations[transform] = transform.rotation;
			_unsnappedTranslations[transform] += transform.position - oldPos;	// The rotation also added translation, so calculate the translation delta.

			// Perform snapping.
			transform.position = SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], SnapDistance) : _unsnappedTranslations[transform];
			transform.rotation = SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], SnapAngle) : _unsnappedRotations[transform];
		}

		// Move the scene camera.
		Fly(sceneView, GrabMoveInvertTranslation, GrabMoveInvertRotation);
	}
	public static void StraightenHorizon() {
		_camera.rotation = Quaternion.Euler(_camera.rotation.eulerAngles.x, _camera.rotation.eulerAngles.y, 0);

		// Update sceneview pivot and repaint view.
		SceneView.lastActiveSceneView.pivot = _pivot.position;
		SceneView.lastActiveSceneView.rotation = _pivot.rotation;
		SceneView.lastActiveSceneView.Repaint();
	}
	#endregion - Navigation -
    
	#region - Dummy Camera Rig -
	/// <summary>
	/// Sets up a dummy camera rig like the scene camera.
	/// We can't move the camera, only the SceneView's pivot & rotation.
	/// For some reason the camera does not always have the same position offset to the pivot.
	/// This offset is unpredictable, so we have to update our dummy rig each time before using it.
	/// </summary>
	private static void InitCameraRig() {
		// Create camera rig if one is not already present.
		if (!_pivotGO) {
			_cameraGO = new GameObject("Scene camera dummy") { hideFlags = HideFlags.HideAndDontSave };
			_camera = _cameraGO.transform;

			_pivotGO = new GameObject("Scene camera pivot dummy") { hideFlags = HideFlags.HideAndDontSave };
			_pivot = _pivotGO.transform;
			_pivot.parent = _camera;
		}

		SyncRigWithScene();
	}
	/// <summary>
	/// Position the dummy camera rig like the scene view camera.
	/// </summary>
	private static void SyncRigWithScene() {
		if (SceneView.lastActiveSceneView) {
			_camera.position = SceneView.lastActiveSceneView.camera.transform.position;	// <- this value changes w.r.t. pivot !
			_camera.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
			_pivot.position = SceneView.lastActiveSceneView.pivot;
			_pivot.rotation = SceneView.lastActiveSceneView.rotation;
		}
	}
	private static void DisposeCameraRig() {
		Object.DestroyImmediate(_cameraGO);
		Object.DestroyImmediate(_pivotGO);
	}
	#endregion - Dummy Camera Rig -
    
	#region - Snapping -
	public static void StoreSelectionTransforms() {
		_unsnappedRotations.Clear();
		_unsnappedTranslations.Clear();
		foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
			_unsnappedRotations.Add(transform, transform.rotation);
			_unsnappedTranslations.Add(transform, transform.position);
		}
	}
	private static Quaternion SnapOnRotation(Quaternion q, float snap) {
		Vector3 euler = q.eulerAngles;
		return Quaternion.Euler(
			Mathf.RoundToInt(euler.x / snap) * snap,
			Mathf.RoundToInt(euler.y / snap) * snap,
			Mathf.RoundToInt(euler.z / snap) * snap);
	}
	private static Vector3 SnapOnTranslation(Vector3 v, float snap) {
		return new Vector3(
			Mathf.RoundToInt(v.x / snap) * snap,
			Mathf.RoundToInt(v.y / snap) * snap,
			Mathf.RoundToInt(v.z / snap) * snap);
	}
	#endregion - Snapping -

	#region - Settings -
	private static void ReadSettings() {
		Mode = (OperationMode)PlayerPrefs.GetInt("Navigation mode", (int)OperationMode.Fly);
		ReadAxisInversions(ref FlyInvertTranslation, ref FlyInvertRotation, "Fly");
		ReadAxisInversions(ref OrbitInvertTranslation, ref OrbitInvertRotation, "Orbit");
		ReadAxisInversions(ref TelekinesisInvertTranslation, ref TelekinesisInvertRotation, "Telekinesis");
		ReadAxisInversions(ref GrabMoveInvertTranslation, ref GrabMoveInvertRotation, "Grab move");

		SpaceNavigator.Instance.ReadSettings();
	}
	private static void ReadAxisInversions(ref Vector3 translation, ref Vector3 rotation, string baseName) {
		translation.x = PlayerPrefs.GetInt(baseName + " invert translation x", 1);
		translation.y = PlayerPrefs.GetInt(baseName + " invert translation y", 1);
		translation.z = PlayerPrefs.GetInt(baseName + " invert translation z", 1);
		rotation.x = PlayerPrefs.GetInt(baseName + " invert rotation x", 1);
		rotation.y = PlayerPrefs.GetInt(baseName + " invert rotation y", 1);
		rotation.z = PlayerPrefs.GetInt(baseName + " invert rotation z", 1);
	}
	public static void WriteSettings() {
		PlayerPrefs.SetInt("Navigation mode", (int)Mode);
		WriteAxisInversions(FlyInvertTranslation, FlyInvertRotation, "Fly");
		WriteAxisInversions(OrbitInvertTranslation, OrbitInvertRotation, "Orbit");
		WriteAxisInversions(TelekinesisInvertTranslation, TelekinesisInvertRotation, "Telekinesis");
		WriteAxisInversions(GrabMoveInvertTranslation, GrabMoveInvertRotation, "Grab move");

		SpaceNavigator.Instance.WriteSettings();
	}
	private static void WriteAxisInversions(Vector3 translation, Vector3 rotation, string baseName) {
		PlayerPrefs.SetInt(baseName + " invert translation x", translation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation y", translation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation z", translation.z < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation x", rotation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation y", rotation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation z", rotation.z < 0 ? -1 : 1);
	}
	#endregion - Settings -
}
