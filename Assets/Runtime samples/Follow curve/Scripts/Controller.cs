using UnityEngine;

public class Controller : MonoBehaviour {
	private Transform _camera;
	private bool _isControllingTorus = true;

	public void Awake() {
		_camera = Camera.mainCamera.transform;
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
		}
		else {
			_camera.transform.Translate(SpaceNavigator.Translation, Space.Self);
			// Perform azimuth in world coordinates.
			//_camera.RotateAround(transform.position, Vector3.up, SpaceNavigator.Rotation.Yaw());
			// Perform pitch in local coordinates.
			//_camera.RotateAround(transform.position, _camera.transform.right, SpaceNavigator.Rotation.Pitch());
		}
	}
}
