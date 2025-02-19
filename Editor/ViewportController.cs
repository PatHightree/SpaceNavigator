#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpaceNavigatorDriver
{
    [InitializeOnLoad]
    [Serializable]
    class ViewportController
    {
        // Damping
        private static Vector3 _oldTranslation;
        private static Vector3 _oldRotation;

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
        private static TimeSpan _saveInterval = new TimeSpan(0, 0, 30);
        private static DateTime _lastSaveTime;
        private static Type GameView;

		private static double _lastRefreshTime;
		private static double _diyDeltaTime;
        private static float _deltaTimeFactor = 400f;
        private static bool _hadFocus;

        static ViewportController() 
        {
			// Set up callbacks.
			EditorApplication.update += Update;
			EditorApplication.pauseStateChanged += PauseStateChanged;

            // Initialize.
            Settings.Read();
            InitCameraRig();
            StoreSelectionTransforms();
        }

        #region - Callbacks -

        private static void PauseStateChanged(PauseState obj)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                Settings.Write();
        }

        public static void OnApplicationQuit()
        {
            Settings.Write();
            DisposeCameraRig();
        }

        #endregion - Callbacks -

        private static void Update()
        {
            if (SpaceNavigatorHID.current == null) return;

            // Autosave settings.
            if (!Application.isPlaying && DateTime.Now - _lastSaveTime > _saveInterval)
            {
                Settings.Write();
                _lastSaveTime = DateTime.Now;
            }

            // Toggle led when Unity editor gains/loses focus
            bool hasFocus = EditorApplication.isFocused;
            if (Settings.ToggleLedWhenFocusChanged && _hadFocus != hasFocus)
            {
                SpaceNavigatorHID.current.SetLEDStatus(hasFocus ? SpaceNavigatorHID.LedStatus.On : SpaceNavigatorHID.LedStatus.Off);
                _hadFocus = hasFocus;
            }
            // Don't navigate if the Unity Editor doesn't have focus.
            if (Settings.OnlyNavWhenUnityHasFocus && !hasFocus) return;
            // If we don't want the driver to navigate the editor at runtime, exit now.
            if (Application.isPlaying && !Settings.RuntimeEditorNav) return;

            if (GameView == null)
            {
                var assembly = typeof(UnityEditor.EditorWindow).Assembly;
                GameView = assembly.GetType("UnityEditor.GameView");
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (!sceneView) return;

            SyncRigWithScene();

            if (Settings.LockHorizon && !_wasHorizonLocked)
                StraightenHorizon();
            _wasHorizonLocked = Settings.LockHorizon;

            Settings.TranslationDrift ??= SpaceNavigatorHID.current.Translation.ReadValue();
            Settings.RotationDrift ??= SpaceNavigatorHID.current.Rotation.ReadValue();
            
            _diyDeltaTime = EditorApplication.timeSinceStartup - _lastRefreshTime;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            // Debug.Log($"Unity delta time: {Time.deltaTime} diy deltTime: {_diyDeltaTime}");
            
            ReadDeviceData(Settings.Mode, out Vector3 translation, out Vector3 rotation);
            
            // Return if device is idle.
            if (ApproximatelyEqual(translation, Vector3.zero, Settings.TransSensEpsilon) &&
                ApproximatelyEqual(rotation, Vector3.zero, Settings.RotSensEpsilon))
            {
                _wasIdle = true;
                return;
            }

            switch (Settings.Mode)
            {
                case OperationMode.Fly:
                    Fly(sceneView, translation, rotation);
                    break;
                case OperationMode.Orbit:
                    Orbit(sceneView, translation, rotation);
                    break;
                case OperationMode.Telekinesis:
                    // Manipulate the object free from the camera.
                    Telekinesis(sceneView, translation, rotation);
                    break;
                case OperationMode.GrabMove:
                    // Manipulate the object together with the camera.
                    GrabMove(sceneView, translation, rotation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _wasIdle = false;
        }

        #region - Navigation -

        private static void Fly(SceneView sceneView, Vector3 translation, Vector3 rotation)
        {
            SyncRigWithScene();

            _camera.Translate(translation, Space.Self);
            if (sceneView.orthographic)
                sceneView.size -= translation.z;
            else
            {
                if (Settings.LockHorizon)
                {
                    // Perform yaw in world coordinates.
                    _camera.Rotate(Vector3.up, rotation.y, Space.World);
                    // Perform pitch in local coordinates.
                    _camera.Rotate(Vector3.right, rotation.x, Space.Self);
                }
                else
                {
                    // Default rotation method, applies the whole quaternion to the camera.
                    _camera.Rotate(rotation);
                }
            }

            // Update sceneview pivot and repaint view.
            sceneView.pivot = _pivot.position;
            sceneView.rotation = _pivot.rotation;
            sceneView.Repaint();
        }

        private static void Orbit(SceneView sceneView, Vector3 translation, Vector3 rotation)
        {
            // If no object is selected don't orbit, fly instead.
            if (Selection.gameObjects.Length == 0)
            {
                Fly(sceneView, translation, rotation);
                return;
            }

            SyncRigWithScene();
            
            _camera.Translate(translation, Space.Self);

            if (Settings.LockHorizon)
            {
                _camera.RotateAround(Tools.handlePosition, Vector3.up, rotation.y);
                _camera.RotateAround(Tools.handlePosition, _camera.right, rotation.x);
            }
            else
            {
                _camera.RotateAround(Tools.handlePosition, _camera.up, rotation.y);
                _camera.RotateAround(Tools.handlePosition, _camera.right, rotation.x);
                _camera.RotateAround(Tools.handlePosition, _camera.forward, rotation.z);
            }

            // Update sceneview pivot and repaint view.
            sceneView.pivot = _pivot.position;
            sceneView.rotation = _pivot.rotation;
            sceneView.Repaint();
        }

        private static void Telekinesis(SceneView sceneView, Vector3 translation, Vector3 rotation)
        {
            if (_wasIdle)
                Undo.IncrementCurrentGroup();
            Transform[] selection = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            Undo.SetCurrentGroupName("Telekinesis");
            Undo.RecordObjects(selection, "Telekinesis");
            
            Quaternion euler = Quaternion.Euler(rotation);
            
            // Store the selection's transforms because the user could have edited them since we last used them via the inspector.
            if (_wasIdle)
                StoreSelectionTransforms();

            foreach (Transform transform in selection)
            {
                if (!_unsnappedRotations.ContainsKey(transform)) continue;

                Transform reference;
                switch (Settings.CoordSys)
                {
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

                if (reference == null)
                {
                    // Move the object in world coordinates.
                    _unsnappedTranslations[transform] += translation;
                    _unsnappedRotations[transform] = euler * _unsnappedRotations[transform];
                }
                else
                {
                    // Move the object in the reference coordinate system.
                    Vector3 worldTranslation = reference.TransformPoint(translation) - reference.position;
                    _unsnappedTranslations[transform] += worldTranslation;
                    
                    if (Settings.LockHorizon)
                    {
                        // Apply yaw in world space
                        Quaternion yaw = Quaternion.Euler(0f, rotation.y, 0f);
                        _unsnappedRotations[transform] = yaw * _unsnappedRotations[transform];
                        // Apply pitch in reference coordinate system
                        Quaternion pitch = Quaternion.Euler(rotation.x, 0f, 0f);
                        _unsnappedRotations[transform] = (transform.rotation * pitch * Quaternion.Inverse(transform.rotation)) * _unsnappedRotations[transform];
                    }
                    else
                        // Rotate object in reference coordinate system.
                        _unsnappedRotations[transform] = (reference.rotation * euler * Quaternion.Inverse(reference.rotation)) * _unsnappedRotations[transform];
                }

                // Perform rotation with or without snapping.
                transform.rotation = Settings.SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], Settings.SnapAngle) : _unsnappedRotations[transform];
                transform.position = Settings.SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], Settings.SnapDistance) : _unsnappedTranslations[transform];
            }
        }

        private static void GrabMove(SceneView sceneView, Vector3 translation, Vector3 rotation)
        {
            if (_wasIdle)
                Undo.IncrementCurrentGroup();
            Transform[] selection = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            Undo.SetCurrentGroupName("GrabMove");
            Undo.RecordObjects(selection, "GrabMove");
            
            // Store the selection's transforms because the user could have edited them since we last used them via the inspector.
            if (_wasIdle)
                StoreSelectionTransforms();

            foreach (Transform transform in selection)
            {
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
                _unsnappedTranslations[transform] += transform.position - oldPos; // The rotation also added translation, so calculate the translation delta.

                // Perform snapping.
                transform.position = Settings.SnapTranslation ? SnapOnTranslation(_unsnappedTranslations[transform], Settings.SnapDistance) : _unsnappedTranslations[transform];
                transform.rotation = Settings.SnapRotation ? SnapOnRotation(_unsnappedRotations[transform], Settings.SnapAngle) : _unsnappedRotations[transform];
            }

            // Move the scene camera.
            Fly(sceneView, translation, rotation);
        }

        private static void ReadDeviceData(OperationMode mode, out Vector3 translation, out Vector3 rotation)
        {
            // Read data from device
            translation = SpaceNavigatorHID.current.Translation.ReadValue() - Settings.TranslationDrift.Value;
            rotation = SpaceNavigatorHID.current.Rotation.ReadValue() - Settings.RotationDrift.Value;

            // Damping
            if (Settings.PresentationMode)
            {
                translation = Damp(_oldTranslation, translation, Settings.PresentationDamping, (float)_diyDeltaTime);
                rotation = Damp(_oldRotation, rotation, Settings.PresentationDamping, (float)_diyDeltaTime);
                _oldTranslation = translation;
                _oldRotation = rotation;
            }
            
            // Apply inversion of axes for fly/grabmove mode.
            var translationInversion = mode switch
            {
                OperationMode.Fly => Settings.FlyInvertTranslation,
                OperationMode.Orbit => Settings.OrbitInvertTranslation,
                OperationMode.Telekinesis => Settings.TelekinesisInvertTranslation,
                OperationMode.GrabMove => Settings.GrabMoveInvertTranslation,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            var rotationInversion = mode switch
            {
                OperationMode.Fly => Settings.FlyInvertRotation,
                OperationMode.Orbit => Settings.OrbitInvertRotation,
                OperationMode.Telekinesis => Settings.TelekinesisInvertRotation,
                OperationMode.GrabMove => Settings.GrabMoveInvertRotation,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
            translation = Vector3.Scale(translation, translationInversion);
            rotation = Vector3.Scale(rotation, rotationInversion);

            // Make navigation framerate independent
            translation *= (float)_diyDeltaTime * _deltaTimeFactor;
            rotation *= (float)_diyDeltaTime * _deltaTimeFactor;
            
            // Apply sensitivity
            translation *= Settings.TransSens[Settings.CurrentGear];
            rotation *= Settings.RotSens;
            
            // Apply locks
            translation.Scale(Settings.GetLocks(DoF.Translation));
            rotation.Scale(Settings.GetLocks(DoF.Rotation));
        }

        public static Vector3 Damp(Vector3 source, Vector3 target, float smoothing, float dt)
        {
            return Vector3.Lerp(source, target, 1 - Mathf.Pow(smoothing, dt));
        }

        public static void StraightenHorizon()
        {
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
        private static void InitCameraRig()
        {
            _cameraGO = GameObject.Find(CameraName);
            _pivotGO = GameObject.Find(PivotName);
            // Create camera rig if one is not already present.
            if (!_pivotGO)
            {
                _cameraGO = new GameObject(CameraName) {hideFlags = HideFlags.HideAndDontSave};
                _pivotGO = new GameObject(PivotName) {hideFlags = HideFlags.HideAndDontSave};
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
        private static void SyncRigWithScene()
        {
            if (SceneView.lastActiveSceneView)
            {
                _camera.position = SceneView.lastActiveSceneView.camera.transform.position; // <- this value changes w.r.t. pivot !
                _camera.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
                _pivot.position = SceneView.lastActiveSceneView.pivot;
                _pivot.rotation = SceneView.lastActiveSceneView.rotation;
            }
        }

        private static void DisposeCameraRig()
        {
            Object.DestroyImmediate(_cameraGO);
            Object.DestroyImmediate(_pivotGO);
        }

        #endregion - Dummy Camera Rig -

        #region - Snapping -

        public static void StoreSelectionTransforms()
        {
            _unsnappedRotations.Clear();
            _unsnappedTranslations.Clear();
            foreach (Transform transform in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable))
            {
                _unsnappedRotations.Add(transform, transform.rotation);
                _unsnappedTranslations.Add(transform, transform.position);
            }
        }

        private static Quaternion SnapOnRotation(Quaternion q, float snap)
        {
            Vector3 euler = q.eulerAngles;
            return Quaternion.Euler(
                Mathf.RoundToInt(euler.x / snap) * snap,
                Mathf.RoundToInt(euler.y / snap) * snap,
                Mathf.RoundToInt(euler.z / snap) * snap);
        }

        private static Vector3 SnapOnTranslation(Vector3 v, float snap)
        {
            return new Vector3(
                Mathf.RoundToInt(v.x / snap) * snap,
                Mathf.RoundToInt(v.y / snap) * snap,
                Mathf.RoundToInt(v.z / snap) * snap);
        }

        #endregion - Snapping -
        
        #region - Deadzone -
        
        private static bool ApproximatelyEqual(Vector3 lhs, Vector3 rhs, float epsilon)
        {
            float num = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            float num4 = num * num + num2 * num2 + num3 * num3;
            return num4 < (9.99999944f * Mathf.Pow(10, -epsilon));
        }

        #endregion - Deadzone -
    }
}
#endif
