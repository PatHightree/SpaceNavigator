using UnityEngine;
using UnityEditor;


public class SpaceNavigatorWindow : EditorWindow {
	private bool _doViewportUpdate = true;
	private bool _receivedNewData, _receivedNewDataChanged;

	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";
	private GameObject _pivotDummyGO, _cameraDummyGO;
	private Transform _pivotDummy, _cameraDummy;

	/// <summary>
	/// Initializes the window.
	/// </summary>
	[MenuItem("Window/Space Navigator")]
	public static void Init() {
		SpaceNavigatorWindow window = GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
			ReadSettings();
			window.SetUpCameraRig();
		}
	}
	/// <summary>
	/// Called when window is closed.
	/// </summary>
	public void OnDestroy() {
		WriteSettings();
	}

	/// <summary>
	/// Sets up a dummy camera rig like the scene camera.
	/// We can't move the camera, only the SceneView's pivot & rotation.
	/// For some reason the camera does not always have the same position offset to the pivot.
	/// This offset is unpredictable, so we update our dummy rig each time before using it.
	/// </summary>
	private void SetUpCameraRig() {
		// Create camera rig if one is not already present.
		if (!_pivotDummyGO) {
			_cameraDummyGO = new GameObject("Scene camera dummy") {hideFlags = HideFlags.HideAndDontSave};
			_cameraDummy = _cameraDummyGO.transform;

			_pivotDummyGO = new GameObject("Scene camera pivot dummy") {hideFlags = HideFlags.HideAndDontSave};
			_pivotDummy = _pivotDummyGO.transform;
			_pivotDummy.parent = _cameraDummy;
		}

		// Position the dummy camera rig like the scene view camera.
		if (SceneView.lastActiveSceneView) {
			_cameraDummy.position = SceneView.lastActiveSceneView.camera.transform.position;	// <- this value changes w.r.t. pivot !
			_cameraDummy.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
			_pivotDummy.position = SceneView.lastActiveSceneView.pivot;
			_pivotDummy.rotation = SceneView.lastActiveSceneView.rotation;
		}
	}

	/// <summary>
	/// This is called 100x per second.
	/// </summary>
	public void Update() {
		SceneView sceneView = SceneView.lastActiveSceneView;

		if (!sceneView) return;

		if (_doViewportUpdate && SpaceNavigator.Instance.HasNewData) {
			MoveSceneCameraWithDummyRig(sceneView);
			//MoveSceneCameraWithPivotDummy(sceneView);
		}

		// Indicate in EditorWindow whether new data has been received.
		_receivedNewDataChanged = _receivedNewData != SpaceNavigator.Instance.HasNewData;
		_receivedNewData = SpaceNavigator.Instance.HasNewData;
		if (_receivedNewData || _receivedNewDataChanged)
			Repaint();
	}

	/// <summary>
	/// Moves the scene camera by using the dummy rig.
	/// </summary>
	/// <param name="sceneView">The scene view.</param>
	private void MoveSceneCameraWithDummyRig(SceneView sceneView) {
		SetUpCameraRig();

		// Position camera dummy where you'd like the scene camera to be.
		// The pivot dummy ends up where the pivot needs to go.

		// Perform translation.
		_cameraDummy.Translate(SpaceNavigator.Instance.Translation, Space.Self);

		// Perform azimuth in world coordinates.
		Vector3 euler = _cameraDummy.rotation.eulerAngles;
		euler.y += SpaceNavigator.Instance.Rotation.y;
		_cameraDummy.rotation = Quaternion.Euler(euler);

		// Perform pitch in local coordinates.
		Vector3 localEuler = _cameraDummy.localRotation.eulerAngles;
		localEuler.x += SpaceNavigator.Instance.Rotation.x;
		_cameraDummy.localRotation = Quaternion.Euler(localEuler);

		// Update the SceneView.
		sceneView.pivot = _pivotDummy.position;
		sceneView.rotation = _pivotDummy.rotation;
		sceneView.Repaint();
	}
	private void MoveSceneCameraWithPivotDummy(SceneView sceneView) {
		_pivotDummy.position = sceneView.pivot;
		_pivotDummy.rotation = sceneView.rotation;

		// Perform translation.
		_pivotDummy.Translate(SpaceNavigator.Instance.Translation, Space.Self);

		// Perform azimuth in world coordinates.
		Vector3 euler = _pivotDummy.rotation.eulerAngles;
		euler.y += SpaceNavigator.Instance.Rotation.y;
		_pivotDummy.rotation = Quaternion.Euler(euler);

		// Perform pitch in local coordinates.
		Vector3 localEuler = _pivotDummy.localRotation.eulerAngles;
		localEuler.x += SpaceNavigator.Instance.Rotation.x;
		_pivotDummy.localRotation = Quaternion.Euler(localEuler);

		// Update the SceneView.
		sceneView.pivot = _pivotDummy.position;
		sceneView.rotation = _pivotDummy.rotation;
		sceneView.Repaint();

		//Transform camera = sceneView.camera.transform;
		//Vector3 worldSpaceTranslation = SpaceNavigator.Instance.Translation.x * sceneView.camera.transform.right +
		//								SpaceNavigator.Instance.Translation.y * sceneView.camera.transform.up +
		//								SpaceNavigator.Instance.Translation.z * sceneView.camera.transform.forward;
		//sceneView.pivot += worldSpaceTranslation;

		//Quaternion oldRot = sceneView.rotation;
		//Quaternion azimuth = Quaternion.Euler(0, SpaceNavigator.Instance.Rotation.y, 0);
		//Vector3 right = Vector3.right;
		//right = oldRot * right;
		//Quaternion pitch = Quaternion.AngleAxis(SpaceNavigator.Instance.Rotation.x, right);
		//sceneView.rotation *= azimuth;
		//sceneView.rotation *= pitch;

		//// Move pivot by moving camera.
		//Vector3 oldPos = camera.position;
		//camera.Translate(SpaceNavigator.Instance.Translation, Space.Self);
		//Vector3 deltaPos = camera.position - oldPos;
		//sceneView.pivot += deltaPos;

		//Quaternion oldRot = camera.rotation;
		//camera.RotateAround(camera.position, Vector3.up, SpaceNavigator.Instance.Rotation.y);
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
		GUILayout.Toggle(_receivedNewData, "Received new data");

		SceneView sceneView = SceneView.lastActiveSceneView;
		if (GUILayout.Button("Reset")) {
			if (sceneView) {
				sceneView.pivot = new Vector3(0, 0, 0);
				sceneView.rotation = Quaternion.identity;
				sceneView.Repaint();
			}
		}
		if (sceneView && _pivotDummy) {
			GUILayout.Label(string.Format("Scene cam pos\t{0}", SceneView.lastActiveSceneView.camera.transform.position));
			GUILayout.Label(string.Format("Scene pivot dummy lpos\t{0}", _pivotDummy.localPosition));
			GUILayout.Label(string.Format("Scene pivot pos\t{0}", SceneView.lastActiveSceneView.pivot));
		} else {
			GUILayout.Label(string.Format("Scene cam pos\t{0}", Vector3.zero.ToString()));
			GUILayout.Label(string.Format("Scene pivot dummy lpos\t{0}", Vector3.zero.ToString()));
			GUILayout.Label(string.Format("Scene pivot pos\t{0}", Vector3.zero.ToString()));
		}

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("T Sens {0:0.00000}", SpaceNavigator.Instance.TranslationSensitivity));
		SpaceNavigator.Instance.TranslationSensitivity = GUILayout.HorizontalSlider(SpaceNavigator.Instance.TranslationSensitivity, 0.00001f, 0.001f);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(string.Format("R Sens {0:0.00000}", SpaceNavigator.Instance.RotationSensitivity));
		SpaceNavigator.Instance.RotationSensitivity = GUILayout.HorizontalSlider(SpaceNavigator.Instance.RotationSensitivity, 0.00001f, 0.01f);
		GUILayout.EndHorizontal();
		
		GUILayout.EndVertical();
	}

	/// <summary>
	/// Reads the settings.
	/// </summary>
	private static void ReadSettings() {
		if (PlayerPrefs.HasKey(TransSensKey)) {
			SpaceNavigator.Instance.TranslationSensitivity = PlayerPrefs.GetFloat(TransSensKey);
			SpaceNavigator.Instance.RotationSensitivity = PlayerPrefs.GetFloat(RotSensKey);
		}
	}
	/// <summary>
	/// Writes the settings.
	/// </summary>
	private static void WriteSettings() {
		if (SpaceNavigator.HasInstance) {
			PlayerPrefs.SetFloat(TransSensKey, SpaceNavigator.Instance.TranslationSensitivity);
			PlayerPrefs.SetFloat(RotSensKey, SpaceNavigator.Instance.RotationSensitivity);
		}
	}
}