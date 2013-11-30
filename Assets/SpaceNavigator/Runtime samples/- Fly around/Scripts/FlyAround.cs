using UnityEngine;

public class FlyAround : MonoBehaviour {
	public bool HorizonLock = true;

	public void Update () {
		transform.Translate(SpaceNavigator.Translation, Space.Self);

		if (HorizonLock) {
			// This method keeps the horizon horizontal at all times.
			// Perform azimuth in world coordinates.
			transform.Rotate(Vector3.up, SpaceNavigator.Rotation.Yaw());
			// Perform pitch in local coordinates.
			transform.Rotate(transform.right, SpaceNavigator.Rotation.Pitch());
		}
		else {
			transform.Rotate(SpaceNavigator.Rotation.eulerAngles, Space.Self);
		}
	}
}
