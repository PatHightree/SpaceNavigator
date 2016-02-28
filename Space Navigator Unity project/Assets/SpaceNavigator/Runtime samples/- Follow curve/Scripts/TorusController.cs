using UnityEngine;
using SpaceNavigatorDriver;

public class TorusController : MonoBehaviour {
	private Transform _camera;
	private Transform _LookAtTarget;
	private bool _isControllingTorus = true;

	public void Awake() {
		SpaceNavigator.SetTranslationSensitivity(1);
		SpaceNavigator.SetRotationSensitivity(1);

		_camera = Camera.main.transform;
		_LookAtTarget = GameObject.FindGameObjectWithTag("Torus look at target").transform;
	}

	public void Update() {
		if (Input.GetKeyDown(KeyCode.Space))
			_isControllingTorus = !_isControllingTorus;

		if (_isControllingTorus) {
			// Move the torus.
			transform.Translate(_camera.transform.TransformDirection(SpaceNavigator.Translation), Space.World);
			transform.rotation = SpaceNavigator.RotationInLocalCoordSys(_camera) * transform.rotation;

			// Move the camera.
			_camera.Translate(SpaceNavigator.Translation, Space.Self);
			_camera.LookAt(_LookAtTarget);
		} else {
			// Perform azimuth in world coordinates.
			_camera.RotateAround(transform.position, Vector3.up, SpaceNavigator.Rotation.Yaw() * Mathf.Rad2Deg);
			// Perform pitch in local coordinates.
			_camera.RotateAround(transform.position, _camera.transform.right, SpaceNavigator.Rotation.Pitch() * Mathf.Rad2Deg);
		}
	}
}
