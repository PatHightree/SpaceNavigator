using UnityEngine;
using UnityEditor;


public class SpaceNavigatorWindow : EditorWindow {
	private bool _doViewportUpdate = true;
	private bool _reset;

	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";
	private GameObject _dummyGO;
	private Transform _dummy;

	/// <summary>
	/// Initializes the window.
	/// </summary>
	[MenuItem("Window/Space Navigator")]
	public static void Init() {
		SpaceNavigatorWindow window = EditorWindow.GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
			ReadSettings();
			window._dummyGO = new GameObject("camera dummy");
			window._dummy = window._dummyGO.transform;
		}
	}
	/// <summary>
	/// Called when window is closed.
	/// </summary>
	public void OnDestroy() {
		WriteSettings();
	}

	/// <summary>
	/// This is called 100x per second.
	/// </summary>
	public void Update() {
		SceneView sceneView = SceneView.lastActiveSceneView;

		if (!sceneView) return;

		
		if (_reset) {
			sceneView.pivot = new Vector3(0, 1, 0);
			sceneView.rotation = Quaternion.identity;
			sceneView.Repaint();
		}

		if (_doViewportUpdate && SpaceNavigator.Instance.HasNewData) {
			Transform camera = sceneView.camera.transform;
			_dummy.position = sceneView.pivot;
			_dummy.rotation = sceneView.rotation;

			// Perform translation.
			_dummy.Translate(SpaceNavigator.Instance.Translation, Space.Self);

			// Perform azimuth in world coordinates.
			Vector3 euler = _dummy.rotation.eulerAngles;
			euler.y += SpaceNavigator.Instance.Rotation.y;
			_dummy.rotation = Quaternion.Euler(euler);

			// Perform pitch in local coordinates.
			Vector3 localEuler = _dummy.localRotation.eulerAngles;
			localEuler.x += SpaceNavigator.Instance.Rotation.x;
			_dummy.localRotation = Quaternion.Euler(localEuler);

			sceneView.pivot = _dummy.position;
			sceneView.rotation = _dummy.rotation;

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

			sceneView.Repaint();
		}
	}
	/// <summary>
	/// Draws the window's GUI.
	/// </summary>
	public void OnGUI() {
		GUILayout.BeginVertical();

		_doViewportUpdate = GUILayout.Toggle(_doViewportUpdate, "SpaceNavigator viewport control");
		_reset = GUILayout.Button("Reset");

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
			Debug.Log("Reading settings");
		}
	}
	/// <summary>
	/// Writes the settings.
	/// </summary>
	private static void WriteSettings() {
		PlayerPrefs.SetFloat(TransSensKey, SpaceNavigator.Instance.TranslationSensitivity);
		PlayerPrefs.SetFloat(RotSensKey, SpaceNavigator.Instance.RotationSensitivity);
	}
}