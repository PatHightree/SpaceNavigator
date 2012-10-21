using System;
using System.Collections;
using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEngine;
using UnityEditor;


public class SpaceNavigatorWindow : EditorWindow {
	// Device variables
	private Sensor _sensor;
	private Device _device;
	//private Keyboard _keyboard;

	// Rig components
	private GameObject _pivotGO, _cameraGO;
	private Transform _pivot, _camera;

	// Settings
	public float TranslationSensitivity, RotationSensitivity;
	public bool NavigationMode;
	private bool _doViewportUpdate;

	private const float TranslationUpdateThreshold = 0.001f;
	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";
	private const string ModeKey = "Navigation mode";

	/// <summary>
	/// Initializes the window.
	/// </summary>
	[MenuItem("Window/Space Navigator")]
	public static void Init() {
		SpaceNavigatorWindow window = GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
			window.ReadSettings();
			window.InitSpaceNavigator();
			window.InitCameraRig();
		}
	}
	/// <summary>
	/// Called when window is closed.
	/// </summary>
	public void OnDestroy() {
		WriteSettings();
		DisposeCameraRig();
		DisposeSpaceNavigator();
	}

	private void InitSpaceNavigator() {
		try {
			if (_device == null) {
				_device = new DeviceClass();
				_sensor = _device.Sensor;
				//_keyboard = _device.Keyboard;
				_device.LoadPreferences("Unity");
			}
			if (!_device.IsConnected)
				_device.Connect();
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
		Debug.Log("Initialized");
	}

	public void DisposeSpaceNavigator() {
		try {
			if (_device != null && _device.IsConnected) {
				_device.Disconnect();

				Debug.Log("Disconnected");
			}
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
	}

	public Vector3 TranslationInWorldSpace {
		get {
			return (_sensor == null ?
				Vector3.zero :
				new Vector3(
					(float)_sensor.Translation.X,
					(float)_sensor.Translation.Y,
					-(float)_sensor.Translation.Z) *
					TranslationSensitivity);
		}
	}
	public Quaternion RotationInWorldSpace {
		get {
			return (_sensor == null ?
				Quaternion.identity :
				Quaternion.AngleAxis(
					(float)_sensor.Rotation.Angle * RotationSensitivity,
					new Vector3(
						-(float)_sensor.Rotation.X,
						-(float)_sensor.Rotation.Y,
						(float)_sensor.Rotation.Z)));
		}
	}
	public Quaternion RotationInLocalCoordSys(Transform coordSys) {
		return coordSys.rotation * RotationInWorldSpace * Quaternion.Inverse(coordSys.rotation);
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
			_cameraGO = new GameObject("Scene camera dummy") {hideFlags = HideFlags.HideAndDontSave};
			_camera = _cameraGO.transform;

			_pivotGO = new GameObject("Scene camera pivot dummy") {hideFlags = HideFlags.HideAndDontSave};
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
		SceneView sceneView = SceneView.lastActiveSceneView;
		if (!sceneView) return;

		if (NavigationMode) {
			// Navigation mode.
			Navigate(sceneView);
		} else {
			// Manipulation mode.
			foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
				// Translate the selected object in camera-space.
				Vector3 worldTranslation = sceneView.camera.transform.TransformPoint(TranslationInWorldSpace) -
											sceneView.camera.transform.position;
				if (worldTranslation != Vector3.zero)
					transform.Translate(worldTranslation, Space.World);

				// Rotate the selected object in camera-space.
				transform.rotation = RotationInLocalCoordSys(sceneView.camera.transform) * transform.rotation;
			}
		}
	}

	private void Navigate(SceneView sceneView) {
		if (TranslationInWorldSpace == Vector3.zero && RotationInWorldSpace == Quaternion.identity) return;

		SyncRigWithScene();

		_camera.Translate(TranslationInWorldSpace, Space.Self);

		//// Default rotation method, applies the whole quaternion to the camera.
		//Quaternion sceneCamera = sceneView.camera.transform.rotation;
		//Quaternion inputInWorldSpace = RotationInWorldSpace;
		//Quaternion inputInCameraSpace = sceneCamera * inputInWorldSpace * Quaternion.Inverse(sceneCamera);
		//_camera.rotation = inputInCameraSpace * _camera.rotation;

		// This method keeps the horizon horizontal at all times.
		// Perform azimuth in world coordinates.
		Vector3 euler = _camera.rotation.eulerAngles;
		euler.y += RotationInWorldSpace.y;
		_camera.rotation = Quaternion.Euler(euler);
		// Perform pitch in local coordinates.
		Vector3 localEuler = _camera.localRotation.eulerAngles;
		localEuler.x += RotationInWorldSpace.x;
		_camera.localRotation = Quaternion.Euler(localEuler);

		// Update sceneview pivot and repaint view.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}

	/// <summary>
	/// Moves the scene camera by using the dummy rig.
	/// </summary>
	/// <param name="sceneView">The scene view.</param>
	private void MoveSceneCameraWithDummyRig(SceneView sceneView) {
		InitCameraRig();

		// Position camera dummy where you'd like the scene camera to be.
		// The pivot dummy ends up where the pivot needs to go.

		// Perform translation.
		_camera.Translate(TranslationInWorldSpace, Space.Self);

		// Perform azimuth in world coordinates.
		Vector3 euler = _camera.rotation.eulerAngles;
		euler.y += RotationInWorldSpace.y;
		_camera.rotation = Quaternion.Euler(euler);

		// Perform pitch in local coordinates.
		Vector3 localEuler = _camera.localRotation.eulerAngles;
		localEuler.x += RotationInWorldSpace.x;
		_camera.localRotation = Quaternion.Euler(localEuler);

		// Update the SceneView.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();
	}
	private void MoveSceneCameraWithPivotDummy(SceneView sceneView) {
		_pivot.position = sceneView.pivot;
		_pivot.rotation = sceneView.rotation;

		// Perform translation.
		_pivot.Translate(TranslationInWorldSpace, Space.Self);

		// Perform azimuth in world coordinates.
		Vector3 euler = _pivot.rotation.eulerAngles;
		euler.y += RotationInWorldSpace.y;
		_pivot.rotation = Quaternion.Euler(euler);

		// Perform pitch in local coordinates.
		Vector3 localEuler = _pivot.localRotation.eulerAngles;
		localEuler.x += RotationInWorldSpace.x;
		_pivot.localRotation = Quaternion.Euler(localEuler);

		// Update the SceneView.
		sceneView.pivot = _pivot.position;
		sceneView.rotation = _pivot.rotation;
		sceneView.Repaint();

		//Transform camera = sceneView.camera.transform;
		//Vector3 worldSpaceTranslation = Translation.x * sceneView.camera.transform.right +
		//								Translation.y * sceneView.camera.transform.up +
		//								Translation.z * sceneView.camera.transform.forward;
		//sceneView.pivot += worldSpaceTranslation;

		//Quaternion oldRot = sceneView.rotation;
		//Quaternion azimuth = Quaternion.Euler(0, Rotation.y, 0);
		//Vector3 right = Vector3.right;
		//right = oldRot * right;
		//Quaternion pitch = Quaternion.AngleAxis(Rotation.x, right);
		//sceneView.rotation *= azimuth;
		//sceneView.rotation *= pitch;

		//// Move pivot by moving camera.
		//Vector3 oldPos = camera.position;
		//camera.Translate(Translation, Space.Self);
		//Vector3 deltaPos = camera.position - oldPos;
		//sceneView.pivot += deltaPos;

		//Quaternion oldRot = camera.rotation;
		//camera.RotateAround(camera.position, Vector3.up, Rotation.y);
		////camera.RotateAroundLocal();
		//Quaternion deltaRot = camera.rotation*Quaternion.Inverse(oldRot);
		////sceneView.rotation *= deltaRot;

		//sceneView.LookAtDirect(camera.position, Quaternion.identity);
	}

	/// <summary>
	/// Draws the EditorWindow's GUI.
	/// </summary>
	public void OnGUI() {
		GUILayout.BeginVertical();

		_doViewportUpdate = GUILayout.Toggle(_doViewportUpdate, "SpaceNavigator viewport control");
		NavigationMode = GUILayout.Toggle(NavigationMode, "Operate in navigation mode");

		SceneView sceneView = SceneView.lastActiveSceneView;
		if (GUILayout.Button("Reset")) {
			if (sceneView) {
				sceneView.pivot = new Vector3(0, 0, 0);
				sceneView.rotation = Quaternion.identity;
				sceneView.Repaint();
			}
		}
		if (sceneView && _pivot) {
			GUILayout.Label(string.Format("Scene cam pos\t{0}", SceneView.lastActiveSceneView.camera.transform.position));
			GUILayout.Label(string.Format("Scene pivot dummy lpos\t{0}", _pivot.localPosition));
			GUILayout.Label(string.Format("Scene pivot pos\t{0}", SceneView.lastActiveSceneView.pivot));
		} else {
			GUILayout.Label(string.Format("Scene cam pos\t{0}", Vector3.zero.ToString()));
			GUILayout.Label(string.Format("Scene pivot dummy lpos\t{0}", Vector3.zero.ToString()));
			GUILayout.Label(string.Format("Scene pivot pos\t{0}", Vector3.zero.ToString()));
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("T Sens {0:0.00000}", TranslationSensitivity));
		TranslationSensitivity = GUILayout.HorizontalSlider(TranslationSensitivity, 0.001f, 1f);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("R Sens {0:0.00000}", RotationSensitivity));
		RotationSensitivity = GUILayout.HorizontalSlider(RotationSensitivity, 0.001f, 1f);
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}

	/// <summary>
	/// Reads the settings.
	/// </summary>
	private void ReadSettings() {
		TranslationSensitivity = EditorPrefs.GetFloat(TransSensKey);
		RotationSensitivity = EditorPrefs.GetFloat(RotSensKey);
		NavigationMode = EditorPrefs.GetInt(ModeKey) == 1;
	}
	/// <summary>
	/// Writes the settings.
	/// </summary>
	private void WriteSettings() {
		EditorPrefs.SetFloat(TransSensKey, TranslationSensitivity);
		EditorPrefs.SetFloat(RotSensKey, RotationSensitivity);
		EditorPrefs.SetInt(ModeKey, NavigationMode ? 1 : 0);
	}
}