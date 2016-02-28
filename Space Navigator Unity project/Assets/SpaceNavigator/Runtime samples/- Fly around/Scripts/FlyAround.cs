using UnityEngine;
using SpaceNavigatorDriver;

public class FlyAround : MonoBehaviour {
	public bool HorizonLock = true;

	public void Update () {
		transform.Translate(SpaceNavigator.Translation, Space.Self);

		if (HorizonLock) {
			// This method keeps the horizon horizontal at all times.
			// Perform azimuth in world coordinates.
			transform.Rotate(Vector3.up, SpaceNavigator.Rotation.Yaw() * Mathf.Rad2Deg, Space.World);
			// Perform pitch in local coordinates.
			transform.Rotate(Vector3.right, SpaceNavigator.Rotation.Pitch() * Mathf.Rad2Deg, Space.Self);
		}
		else {
			transform.Rotate(SpaceNavigator.Rotation.eulerAngles, Space.Self);
		}
	}
}
