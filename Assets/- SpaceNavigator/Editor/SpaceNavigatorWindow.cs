using UnityEngine;
using UnityEditor;


public class SpaceNavigatorWindow : EditorWindow {
	private bool _doViewportUpdate = true;

	private const string TransSensKey = "Translation sensitivity";
	private const string RotSensKey = "Rotation sensitivity";

	/// <summary>
	/// Initializes the window.
	/// </summary>
	[MenuItem("Window/Space Navigator")]
	public static void Init() {
		SpaceNavigatorWindow window = EditorWindow.GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
			ReadSettings();
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
		
		if (_doViewportUpdate && SpaceNavigator.Instance.HasNewData) {
			Vector3 worldSpaceTranslation = SpaceNavigator.Instance.Translation.x*sceneView.camera.transform.right +
			                                SpaceNavigator.Instance.Translation.y*sceneView.camera.transform.up +
			                                SpaceNavigator.Instance.Translation.z*sceneView.camera.transform.forward;
			sceneView.pivot += worldSpaceTranslation;
			sceneView.rotation *= Quaternion.Euler(SpaceNavigator.Instance.Rotation);
			sceneView.Repaint();
		}
	}
	/// <summary>
	/// Draws the window's GUI.
	/// </summary>
	public void OnGUI() {
		GUILayout.BeginVertical();

		_doViewportUpdate = GUILayout.Toggle(_doViewportUpdate, "SpaceNavigator viewport control");

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