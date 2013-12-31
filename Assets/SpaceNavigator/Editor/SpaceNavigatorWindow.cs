#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SpaceNavigatorWindow : EditorWindow {
	public enum OperationMode { Fly, Orbit, Telekinesis, GrabMove }
	private OperationMode _operationMode;
	private bool _lockHorizon = true;
	public enum CoordinateSystem { Camera, World, Parent, Local }
	private static CoordinateSystem _coordSys;

	// Rig components
	[SerializeField]
	private GameObject _pivotGO, _cameraGO;
	[SerializeField]
	private Transform _pivot, _camera;

	// Snapping
	private Dictionary<Transform, Quaternion> _unsnappedRotations = new Dictionary<Transform, Quaternion>();
	private Dictionary<Transform, Vector3> _unsnappedTranslations = new Dictionary<Transform, Vector3>();
	private bool _snapRotation;
	private int _snapAngle = 45;
	private bool _snapTranslation;
	private float _snapDistance = 0.1f;
	private bool _wasIdle;

	// Inversion
	private Vector3 _flyInvertTranslation, _flyInvertRotation;
	private Vector3 _orbitInvertTranslation, _orbitInvertRotation;
	private Vector3 _telekinesisInvertTranslation, _telekinesisInvertRotation;
	private Vector3 _grabMoveInvertTranslation, _grabMoveInvertRotation;

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
	public void OnEnable() {
		ReadSettings();
		InitCameraRig();
		StoreSelectionTransforms();
	}
	public void OnDisable() {
		// This avoids unwanted disposing when playing with "Maximize on Play" enabled on the Game View.
		if (Application.isPlaying) return;

		WriteSettings();
		DisposeCameraRig();
		SpaceNavigator.Instance.Dispose();
	}
	public void OnSelectionChange() {
		StoreSelectionTransforms();
	}

	private void ReadSettings() {
		_operationMode = (OperationMode)PlayerPrefs.GetInt("Navigation mode", (int)OperationMode.Fly);
		ReadAxisInversions(ref _flyInvertTranslation, ref _flyInvertRotation, "Fly");
		ReadAxisInversions(ref _orbitInvertTranslation, ref _orbitInvertRotation, "Orbit");
		ReadAxisInversions(ref _telekinesisInvertTranslation, ref _telekinesisInvertRotation, "Telekinesis");
		ReadAxisInversions(ref _grabMoveInvertTranslation, ref _grabMoveInvertRotation, "Grab move");

		SpaceNavigator.Instance.ReadSettings();
	}
	private void ReadAxisInversions(ref Vector3 translation, ref Vector3 rotation, string baseName) {
		translation.x = PlayerPrefs.GetInt(baseName + " invert translation x", 1);
		translation.y = PlayerPrefs.GetInt(baseName + " invert translation y", 1);
		translation.z = PlayerPrefs.GetInt(baseName + " invert translation z", 1);
		rotation.x = PlayerPrefs.GetInt(baseName + " invert rotation x", 1);
		rotation.y = PlayerPrefs.GetInt(baseName + " invert rotation y", 1);
		rotation.z = PlayerPrefs.GetInt(baseName + " invert rotation z", 1);
	}
	private void WriteSettings() {
		PlayerPrefs.SetInt("Navigation mode", (int)_operationMode);
		WriteAxisInversions(_flyInvertTranslation, _flyInvertRotation, "Fly");
		WriteAxisInversions(_orbitInvertTranslation, _orbitInvertRotation, "Orbit");
		WriteAxisInversions(_telekinesisInvertTranslation, _telekinesisInvertRotation, "Telekinesis");
		WriteAxisInversions(_grabMoveInvertTranslation, _grabMoveInvertRotation, "Grab move");

		SpaceNavigator.Instance.WriteSettings();
	}
	private void WriteAxisInversions(Vector3 translation, Vector3 rotation, string baseName) {
		PlayerPrefs.SetInt(baseName + " invert translation x", translation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation y", translation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert translation z", translation.z < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation x", rotation.x < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation y", rotation.y < 0 ? -1 : 1);
		PlayerPrefs.SetInt(baseName + " invert rotation z", rotation.z < 0 ? -1 : 1);
	}

	/// <summary>
	/// Sets up a dummy camera rig like the scene camera.
	/// We can't move the camera, only the SceneView's pivot & rotation.
	/// For some reason the camera does not always have the same position offset to the pivot.
	/// This offset is unpredictable, so we have to update our dummy rig each time before using it.
	/// </summary>
	private void InitCameraRig() {
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
	private void SyncRigWithScene() {
		if (SceneView.lastActiveSceneView) {
			_camera.position = SceneView.lastActiveSceneView.camera.transform.position;	// <- this value changes w.r.t. pivot !
			_camera.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
			_pivot.position = SceneView.lastActiveSceneView.pivot;
			_pivot.rotation = SceneView.lastActiveSceneView.rotation;
		}
	}
	private void DisposeCameraRig() {
		DestroyImmediate(_cameraGO);
		DestroyImmediate(_pivotGO);
	}

	/// <summary>
	/// This is called 100x per second (if the window content is visible).
	/// </summary>
	public void Update() {
		// Fly mode should not be impaired by locked axes.
		SpaceNavigator.IsLockingAllowed = _operationMode != OperationMode.Fly;

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

		switch (_operationMode) {
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

	private void Fly(SceneView sceneView) {
		Fly(sceneView, _flyInvertTranslation, _flyInvertRotation);
	}

	private void Fly(SceneView sceneView, Vector3 translationInversion, Vector3 rotationInversion) {
		SyncRigWithScene();

		// Apply inversion of axes for fly/grabmove mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, translationInversion);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, rotationInversion);

		_camera.Translate(translation, Space.Self);

		if (_lockHorizon) {
			// Perform azimuth in world coordinates.
			_camera.Rotate(Vector3.up, rotation.y, Space.World);
			// Perform pitch in local coordinates.
			_camera.Rotate(Vector3.right, rotation.x, Space.Self);
		}
		else {
			// Default rotation method, applies the whole quaternion to the camera.
			_camera.rotation *= SpaceNavigator.Rotation;
			_camera.Rotate(Vector3.up, rotation.y, Space.Self);
			_camera.Rotate(Vector3.right, rotation.x, Space.Self);
			_camera.Rotate(Vector3.forward, rotation.z, Space.Self);
		}

		// Update sceneview pivot and repaint view.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}
	private void Orbit(SceneView sceneView) {
		if (Selection.gameObjects.Length == 0) return;

		SyncRigWithScene();

		// Apply inversion of axes for orbit mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, _orbitInvertTranslation);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, _orbitInvertRotation);

		_camera.Translate(new Vector3(0, 0, translation.z), Space.Self);

		if (_lockHorizon) {
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
	private void Telekinesis(SceneView sceneView) {
		// Apply inversion of axes for telekinesis mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, _telekinesisInvertTranslation);
		Quaternion rotation = Quaternion.Euler(Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, _telekinesisInvertRotation));

		// Store the selection's transforms because the user could have edited them since we last used them via the inspector.
		if (_wasIdle)
			StoreSelectionTransforms();

		foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
			if (!_unsnappedRotations.ContainsKey(transform)) continue;

			Transform reference;
			switch (_coordSys) {
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
			transform.rotation = _snapRotation ? SnapRotation(_unsnappedRotations[transform], _snapAngle) : _unsnappedRotations[transform];
			transform.position = _snapTranslation ? SnapTranslation(_unsnappedTranslations[transform], _snapDistance) : _unsnappedTranslations[transform];
		}
	}
	private void GrabMove(SceneView sceneView) {
		// Apply inversion of axes for grab move mode.
		Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, _grabMoveInvertTranslation);
		Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, _grabMoveInvertRotation);

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
			transform.position = _snapTranslation ? SnapTranslation(_unsnappedTranslations[transform], _snapDistance) : _unsnappedTranslations[transform];
			transform.rotation = _snapRotation ? SnapRotation(_unsnappedRotations[transform], _snapAngle) : _unsnappedRotations[transform];
		}

		// Move the scene camera.
		Fly(sceneView, _grabMoveInvertTranslation, _grabMoveInvertRotation);
	}

	private void StraightenHorizon() {
		_camera.rotation = Quaternion.Euler(_camera.rotation.eulerAngles.x, _camera.rotation.eulerAngles.y, 0);

		// Update sceneview pivot and repaint view.
		SceneView.lastActiveSceneView.pivot = _pivot.position;
		SceneView.lastActiveSceneView.rotation = _pivot.rotation;
		SceneView.lastActiveSceneView.Repaint();
	}

	/// <summary>
	/// Draws the EditorWindow's GUI.
	/// </summary>
	public void OnGUI() {
		GUILayout.BeginVertical();

		#region - Operation mode -
		GUILayout.Label("Operation mode");
		GUIContent[] modes = new GUIContent[] {
			new GUIContent("Fly", "Where do you want to fly today?"),
			new GUIContent("Orbit", "Round, round, round we go"),
			new GUIContent("Telekinesis", "Watch where you're levitating that piano!"),
			new GUIContent("Grab Move", "Excuse me, yes. HDS coming through. I've got a package people")
		};
		_operationMode = (OperationMode)GUILayout.SelectionGrid((int)_operationMode, modes, 4);
		#endregion - Operation mode -

		#region - Coordinate system -
		// Enable the coordsys only in Telekinesis mode.
		GUI.enabled = _operationMode == OperationMode.Telekinesis;
		GUILayout.Label("Coordinate system");
		string[] coordSystems = new string[] { "Camera", "World", "Parent", "Local" };
		_coordSys = (CoordinateSystem)GUILayout.SelectionGrid((int)_coordSys, coordSystems, 4);
		#endregion - Coordinate system -

		#region - Snapping -
		// Disable the constraint controls in Fly and Orbit mode.
		GUI.enabled = _operationMode != OperationMode.Fly && _operationMode != OperationMode.Orbit;

		GUILayout.Space(10);
		GUILayout.Label("Snap");
		GUILayout.Space(4);
		GUILayout.BeginHorizontal();
		_snapTranslation = GUILayout.Toggle(_snapTranslation, "Grid snap");
		_snapDistance = EditorGUILayout.FloatField(_snapDistance);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		_snapRotation = GUILayout.Toggle(_snapRotation, "Angle snap");
		_snapAngle = EditorGUILayout.IntField(_snapAngle);
		GUILayout.EndHorizontal();

		// Re-enable gui.
		GUI.enabled = true;
		#endregion- Snapping -

		#region - Locking and sensitivity -
		GUILayout.Space(10);
		GUILayout.Label("Lock");
		GUILayout.Space(4);

		EditorGUI.BeginChangeCheck();
		_lockHorizon = GUILayout.Toggle(_lockHorizon, "Horizon");
		if (EditorGUI.EndChangeCheck() && _lockHorizon)
			StraightenHorizon();

		SpaceNavigator.Instance.OnGUI();
		#endregion - Locking and sensitivity -

		#region - Axes inversion per mode -
		GUILayout.Space(10);
		GUILayout.Label("Invert axes in " + _operationMode.ToString() + " mode");
		GUILayout.Space(4);

		bool tx, ty, tz, rx, ry, rz;
		switch (_operationMode) {
			case OperationMode.Fly:
				tx = _flyInvertTranslation.x < 0; ty = _flyInvertTranslation.y < 0; tz = _flyInvertTranslation.z < 0;
				rx = _flyInvertRotation.x < 0; ry = _flyInvertRotation.y < 0; rz = _flyInvertRotation.z < 0;
				break;
			case OperationMode.Orbit:
				tx = _orbitInvertTranslation.x < 0; ty = _orbitInvertTranslation.y < 0; tz = _orbitInvertTranslation.z < 0;
				rx = _orbitInvertRotation.x < 0; ry = _orbitInvertRotation.y < 0; rz = _orbitInvertRotation.z < 0;
				break;
			case OperationMode.Telekinesis:
				tx = _telekinesisInvertTranslation.x < 0; ty = _telekinesisInvertTranslation.y < 0; tz = _telekinesisInvertTranslation.z < 0;
				rx = _telekinesisInvertRotation.x < 0; ry = _telekinesisInvertRotation.y < 0; rz = _telekinesisInvertRotation.z < 0;
				break;
			case OperationMode.GrabMove:
				tx = _grabMoveInvertTranslation.x < 0; ty = _grabMoveInvertTranslation.y < 0; tz = _grabMoveInvertTranslation.z < 0;
				rx = _grabMoveInvertRotation.x < 0; ry = _grabMoveInvertRotation.y < 0; rz = _grabMoveInvertRotation.z < 0;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label("Translation\t\t");
		EditorGUI.BeginChangeCheck();
		tx = GUILayout.Toggle(tx, "X");
		ty = GUILayout.Toggle(ty, "Y");
		tz = GUILayout.Toggle(tz, "Z");
		if (EditorGUI.EndChangeCheck()) {
			switch (_operationMode) {
				case OperationMode.Fly:
					_flyInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					_orbitInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					_telekinesisInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					_grabMoveInvertTranslation = new Vector3(tx ? -1 : 1, ty ? -1 : 1, tz ? -1 : 1);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Rotation\t\t\t");
		EditorGUI.BeginChangeCheck();

		rx = GUILayout.Toggle(rx, "X");
		ry = GUILayout.Toggle(ry, "Y");
		rz = GUILayout.Toggle(rz, "Z");
		if (EditorGUI.EndChangeCheck()) {
			switch (_operationMode) {
				case OperationMode.Fly:
					_flyInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Orbit:
					_orbitInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.Telekinesis:
					_telekinesisInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				case OperationMode.GrabMove:
					_grabMoveInvertRotation = new Vector3(rx ? -1 : 1, ry ? -1 : 1, rz ? -1 : 1);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		GUILayout.EndHorizontal();
		#endregion - Axes inversion per mode -

		GUILayout.EndVertical();
	}

	#region - Snapping -
	public void StoreSelectionTransforms() {
		_unsnappedRotations.Clear();
		_unsnappedTranslations.Clear();
		foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
			_unsnappedRotations.Add(transform, transform.rotation);
			_unsnappedTranslations.Add(transform, transform.position);
		}
	}
	private Quaternion SnapRotation(Quaternion q, float snap) {
		Vector3 euler = q.eulerAngles;
		return Quaternion.Euler(
			Mathf.RoundToInt(euler.x / snap) * snap,
			Mathf.RoundToInt(euler.y / snap) * snap,
			Mathf.RoundToInt(euler.z / snap) * snap);
	}
	private Vector3 SnapTranslation(Vector3 v, float snap) {
		return new Vector3(
			Mathf.RoundToInt(v.x / snap) * snap,
			Mathf.RoundToInt(v.y / snap) * snap,
			Mathf.RoundToInt(v.z / snap) * snap);
	}
	#endregion - Snapping -
}
#endif