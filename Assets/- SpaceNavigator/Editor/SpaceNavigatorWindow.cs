using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public class SpaceNavigatorWindow : EditorWindow {
	static List<SceneView> _svList = new List<SceneView>();
	private Transform _dummy, _parent;
	private bool _moveUp, _moveDown, _doViewportUpdate;

	[MenuItem("Window/Space Navigator")]
	public static void Init() {
		SpaceNavigatorWindow window = EditorWindow.GetWindow(typeof(SpaceNavigatorWindow)) as SpaceNavigatorWindow;

		if (window) {
			window.Show();
			window.RefreshList();
		}
		_svList = new List<SceneView>();
	}

	public void RefreshList() {
		Object[] sceneviews = Resources.FindObjectsOfTypeAll(typeof(SceneView));

		_svList.Clear();
		foreach (Object o in sceneviews)
			_svList.Add(o as SceneView);

		Repaint();
	}

	// Is called 100x per second.
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

	public void OnGUI() {
		SceneView sv = SceneView.lastActiveSceneView;
		//foreach (SceneView sv in _svList) {
			GUILayout.BeginVertical();
			GUILayout.Label(SceneView.lastActiveSceneView!=null ? SceneView.lastActiveSceneView.camera.transform.rotation.eulerAngles.ToString() : "");
			_doViewportUpdate = GUILayout.Toggle(_doViewportUpdate, "SpaceNavigator viewport control");

			if (GUILayout.Button("Look at 0,0,0")) {
				sv.LookAt(new Vector3(0, 0, 0));
				sv.Repaint();
			}

			if (GUILayout.Button("Rot 0,0,0")) {
				sv.rotation = Quaternion.Euler(0, 0, 0);
				sv.Repaint();
			}

			if (GUILayout.Button("Move view up")) {
				sv.pivot += new Vector3(0, 0.1f, 0);
				sv.Repaint();
			}
			//Object cube = FindObjectsOfType(typeof(GameObject))
			//    .ToList()
			//    .Where( go => go.name == "Cube")
			//    .First();

			if (GUILayout.Button("Rotate view")) {
				Vector3 v = sv.rotation.eulerAngles;
				v.y += 10;
				sv.rotation = Quaternion.Euler(v);

				Matrix4x4 mat = new Matrix4x4();
				mat.SetTRS(Vector3.zero, sv.rotation, Vector3.one);
				mat = mat.inverse;


				//_dummy.position = sv.camera.transform.position;
				//_dummy.rotation = sv.camera.transform.rotation;
				//sv.pivot = _dummy.
				//sv.pivot = Vector3. new Vector3(0, 0.1f, 0);

				//Vector3 pos = sv.camera.transform.position;
				//sv.camera.transform.RotateAround(Vector3.up, 10);
				//Debug.Log(pos == sv.camera.transform.position ? "identical" : "different");
				
				sv.Repaint();
			}


			GUILayout.EndVertical();
		//}

		//if (GUILayout.Button("Refresh view list"))
		//	RefreshList();
	}
}