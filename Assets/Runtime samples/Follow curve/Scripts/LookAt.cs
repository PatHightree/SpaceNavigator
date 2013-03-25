using UnityEngine;

public class LookAt : MonoBehaviour {
	public Transform Target;

	public void LateUpdate () {
		transform.LookAt(Target);
	}
}
