using UnityEngine;

public class RuntimeAccess : MonoBehaviour {
	public void Update () {
		transform.Translate(SpaceNavigator.TranslationInWorldSpace, Space.World);
		transform.Rotate(SpaceNavigator.RotationInWorldSpace.eulerAngles, Space.World);
	}
}
