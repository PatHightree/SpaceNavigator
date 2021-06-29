using UnityEngine;
using SpaceNavigatorDriver;
using UnityEngine.InputSystem;

public class TorusController : MonoBehaviour {
	private Transform _camera;
	private Transform _LookAtTarget;
	private bool _isControllingTorus = true;

	public void Awake()
	{
		Settings.PlayTransSens = 1;
		Settings.PlayRotSens = 1;

		_camera = Camera.main.transform;
		_LookAtTarget = GameObject.FindGameObjectWithTag("Torus look at target").transform;
	}

	public void Update() {
		if (Keyboard.current.spaceKey.wasPressedThisFrame)
			_isControllingTorus = !_isControllingTorus;

		if (_isControllingTorus) {
			// Move the torus.
			transform.Translate(_camera.transform.TransformDirection(SpaceNavigatorHID.current.Translation.ReadValue()), Space.World);
			transform.rotation = RotationInLocalCoordSys(_camera) * transform.rotation;

			// Move the camera.
			_camera.Translate(SpaceNavigatorHID.current.Translation.ReadValue(), Space.Self);
			_camera.LookAt(_LookAtTarget);
		} else {
			// Perform azimuth in world coordinates.
			_camera.RotateAround(transform.position, Vector3.up, SpaceNavigatorHID.current.Rotation.ReadValue().y * Mathf.Rad2Deg);
			// Perform pitch in local coordinates.
			_camera.RotateAround(transform.position, _camera.transform.right, SpaceNavigatorHID.current.Rotation.ReadValue().x * Mathf.Rad2Deg);
		}
	}
	
	public static Quaternion RotationInLocalCoordSys(Transform coordSys) {
		return coordSys.rotation * Quaternion.Euler(SpaceNavigatorHID.current.Rotation.ReadValue()) * Quaternion.Inverse(coordSys.rotation);
	}	
}
