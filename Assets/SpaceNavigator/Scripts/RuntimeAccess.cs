using UnityEngine;

public class RuntimeAccess : MonoBehaviour {
	public void Update () {
		transform.Translate(SpaceNavigator.Translation, Space.World);
		transform.Rotate(SpaceNavigator.Rotation.eulerAngles, Space.World);
	}
}
