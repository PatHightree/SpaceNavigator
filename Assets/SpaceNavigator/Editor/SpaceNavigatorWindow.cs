#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class SpaceNavigatorWindow : EditorWindow {
	public enum OperationMode { Fly, Telekinesis, GrabMove }
	private OperationMode _operationMode;
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

	// Settings
	private const string ModeKey = "Navigation mode";
	private const OperationMode OperationModeDefault = OperationMode.Fly;

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
		_operationMode = (OperationMode)PlayerPrefs.GetInt(ModeKey, (int)OperationModeDefault);

		SpaceNavigator.Instance.ReadSettings();
	}
	private void WriteSettings() {
		PlayerPrefs.SetInt(ModeKey, (int)_operationMode);

		SpaceNavigator.Instance.WriteSettings();
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
		SyncRigWithScene();

		_camera.Translate(SpaceNavigator.Translation, Space.Self);

		//// Default rotation method, applies the whole quaternion to the camera.
		//Quaternion sceneCamera = sceneView.camera.transform.rotation;
		//Quaternion inputInWorldSpace = RotationInWorldSpace;
		//Quaternion inputInCameraSpace = sceneCamera * inputInWorldSpace * Quaternion.Inverse(sceneCamera);
		//_camera.rotation = inputInCameraSpace * _camera.rotation;

		// This method keeps the horizon horizontal at all times.
		// Perform azimuth in world coordinates.
		_camera.Rotate(Vector3.up, SpaceNavigator.Rotation.Yaw() * Mathf.Rad2Deg, Space.World);
		// Perform pitch in local coordinates.
		_camera.Rotate(Vector3.right, SpaceNavigator.Rotation.Pitch() * Mathf.Rad2Deg, Space.Self);

		// Update sceneview pivot and repaint view.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}
	private void Telekinesis(SceneView sceneView) {
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
				_unsnappedTranslations[transform] += SpaceNavigator.Translation;
				_unsnappedRotations[transform] = SpaceNavigator.Rotation*_unsnappedRotations[transform];
			} else {
				// Move the object in the reference coordinate system.
				Vector3 worldTranslation = reference.TransformPoint(SpaceNavigator.Translation) -
										   reference.position;
				_unsnappedTranslations[transform] += worldTranslation;
				_unsnappedRotations[transform] = SpaceNavigator.RotationInLocalCoordSys(reference) * _unsnappedRotations[transform];
			}

			// Perform rotation with or without snapping.
			transform.rotation = _snapRotation ? SnapRotation(_unsnappedRotations[transform], _snapAngle) : _unsnappedRotations[transform];
			transform.position = _snapTranslation ? SnapTranslation(_unsnappedTranslations[transform], _snapDistance) : _unsnappedTranslations[transform];
		}
	}
	private void GrabMove(SceneView sceneView) {
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
			transform.RotateAround(_camera.position, Vector3.up, SpaceNavigator.Rotation.Yaw() * Mathf.Rad2Deg);
			transform.RotateAround(_camera.position, _camera.right, SpaceNavigator.Rotation.Pitch() * Mathf.Rad2Deg);

			// Interpret SpaceNavigator input in camera space, calculate the effect in world space.
			Vector3 worldTranslation = sceneView.camera.transform.TransformPoint(SpaceNavigator.Translation) -
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
		Fly(sceneView);
	}

	/// <summary>
	/// Draws the EditorWindow's GUI.
	/// </summary>
	public void OnGUI() {
		GUILayout.BeginVertical();
		GUILayout.Label("Operation mode");
		GUIContent[] modes = new GUIContent[] {
			new GUIContent("Fly", "Where do you want to fly today?"),
			new GUIContent("Telekinesis", "Watch where you're levitating that piano!"),
			new GUIContent("Grab Move", "Excuse me, yes. HDS coming through. I've got a package people")
		};
		_operationMode = (OperationMode)GUILayout.SelectionGrid((int)_operationMode, modes, 3);

		// Enable the coordsys only in Telekinesis mode.
		GUI.enabled = _operationMode == OperationMode.Telekinesis;
		GUILayout.Label("Coordinate system");
		string[] coordSystems = new string[] { "Camera", "World", "Parent", "Local" };
		_coordSys = (CoordinateSystem)GUILayout.SelectionGrid((int)_coordSys, coordSystems, 4);

		// Disable the constraint controls in Fly mode.
		GUI.enabled = _operationMode != OperationMode.Fly;
		GUILayout.Space(10);
		GUILayout.Label("Snapping");
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

		SpaceNavigator.Instance.OnGUI();

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