using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceNavigatorDriver {

	[InitializeOnLoad]
	[Serializable]
	class ViewportController {

		// Snapping
		private static Dictionary<Transform, Quaternion> _unsnappedRotations = new Dictionary<Transform, Quaternion>();
		private static Dictionary<Transform, Vector3> _unsnappedTranslations = new Dictionary<Transform, Vector3>();
		private static bool _wasIdle;

		// Rig components
		private static GameObject _pivotGO, _cameraGO;
		private static Transform _pivot, _camera;
		private const string PivotName = "Scene camera pivot dummy";
		private const string CameraName = "Scene camera dummy";

		private static bool _wasHorizonLocked;
		private const float _saveInterval = 30;
		private static float _lastSaveTime;

		static ViewportController() {
			// Set up callbacks.
			EditorApplication.update += Update;
			EditorApplication.playmodeStateChanged += PlaymodeStateChanged;

			// Initialize.
			Settings.Read();
			InitCameraRig();
			StoreSelectionTransforms();
		}

		#region - Callbacks -
		private static void PlaymodeStateChanged() {
			if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
				Settings.Write();
		}

		public static void OnApplicationQuit() {
			Settings.Write();
			DisposeCameraRig();
			SpaceNavigator.Instance.Dispose();
		}
		#endregion - Callbacks -

		static void Update() {
			// Autosave settings.
			if (!Application.isPlaying && DateTime.Now.Second - _lastSaveTime > _saveInterval) {
				Settings.Write();
				_lastSaveTime = DateTime.Now.Second;
			}

			// If we don't want the driver to navigate the editor at runtime, exit now.
			if (Application.isPlaying && !Settings.RuntimeEditorNav) return;

			SceneView sceneView = SceneView.lastActiveSceneView;
			if (!sceneView) return;

			if (Settings.LockHorizon && !_wasHorizonLocked)
				StraightenHorizon();
			_wasHorizonLocked = Settings.LockHorizon;

			// Return if device is idle.
			if (SpaceNavigator.Translation == Vector3.zero &&
				SpaceNavigator.Rotation == Quaternion.identity) {
				_wasIdle = true;
				return;
			}

			switch (Settings.Mode) {
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
			Fly(sceneView, Settings.FlyInvertTranslation, Settings.FlyInvertRotation);
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
				if (Settings.LockHorizon) {
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
			Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, Settings.OrbitInvertTranslation);
			Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, Settings.OrbitInvertRotation);

			_camera.Translate(translation, Space.Self);

			if (Settings.LockHorizon) {
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
			Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, Settings.TelekinesisInvertTranslation);
			Quaternion rotation = Quaternion.Euler(Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, Settings.TelekinesisInvertRotation));

			// Store the selection's transforms because the user could have edited them since we last used them via the inspector.
			if (_wasIdle)
				StoreSelectionTransforms();

			foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable)) {
				if (!_unsnappedRotations.ContainsKey(transform)) continue;

				Transform reference;
				switch (Settings.CoordSys) {
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
				transform.rotation = Settings.SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], Settings.SnapAngle) : _unsnappedRotations[transform];
				transform.position = Settings.SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], Settings.SnapDistance) : _unsnappedTranslations[transform];
			}
		}
		static void GrabMove(SceneView sceneView) {
			// Apply inversion of axes for grab move mode.
			Vector3 translation = Vector3.Scale(SpaceNavigator.Translation, Settings.GrabMoveInvertTranslation);
			Vector3 rotation = Vector3.Scale(SpaceNavigator.Rotation.eulerAngles, Settings.GrabMoveInvertRotation);

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
				_unsnappedTranslations[transform] += transform.position - oldPos;   // The rotation also added translation, so calculate the translation delta.

				// Perform snapping.
				transform.position = Settings.SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], Settings.SnapDistance) : _unsnappedTranslations[transform];
				transform.rotation = Settings.SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], Settings.SnapAngle) : _unsnappedRotations[transform];
			}

			// Move the scene camera.
			Fly(sceneView, Settings.GrabMoveInvertTranslation, Settings.GrabMoveInvertRotation);
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
			_cameraGO = GameObject.Find(CameraName);
			_pivotGO = GameObject.Find(PivotName);
			// Create camera rig if one is not already present.
			if (!_pivotGO) {
				_cameraGO = new GameObject(CameraName) { hideFlags = HideFlags.HideAndDontSave };
				_pivotGO = new GameObject(PivotName) { hideFlags = HideFlags.HideAndDontSave };
			}
			// Reassign these variables, they get destroyed when entering play mode.
			_camera = _cameraGO.transform;
			_pivot = _pivotGO.transform;
			_pivot.parent = _camera;

			SyncRigWithScene();
		}
		/// <summary>
		/// Position the dummy camera rig like the scene view camera.
		/// </summary>
		private static void SyncRigWithScene() {
			if (SceneView.lastActiveSceneView) {
				_camera.position = SceneView.lastActiveSceneView.camera.transform.position; // <- this value changes w.r.t. pivot !
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
	}
}