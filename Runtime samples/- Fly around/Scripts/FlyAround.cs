using UnityEngine;
using SpaceNavigatorDriver;

public class FlyAround : MonoBehaviour {
	public bool HorizonLock = true;

	public void Update () {
		transform.Translate(SpaceNavigatorHID.current.Translation.ReadValue(), Space.Self);

		if (HorizonLock) {
			// This method keeps the horizon horizontal at all times.
			// Perform azimuth in world coordinates.
			transform.Rotate(Vector3.up, SpaceNavigatorHID.current.Rotation.ReadValue().y, Space.World);
			// Perform pitch in local coordinates.
			transform.Rotate(Vector3.right, SpaceNavigatorHID.current.Rotation.ReadValue().x, Space.Self);
		}
		else {
			transform.Rotate(SpaceNavigatorHID.current.Rotation.ReadValue(), Space.Self);
		}
	}
}
