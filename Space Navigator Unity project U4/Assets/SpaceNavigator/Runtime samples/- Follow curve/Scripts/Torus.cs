using UnityEngine;

public class Torus : MonoBehaviour {
	private Rigidbody _controller;
	
	public void Awake() {
		_controller = GameObject.FindGameObjectWithTag("Torus controller").rigidbody;
	}

	public void OnCollisionEnter(Collision collision) {
		audio.Play();

		// Move the torus.
		rigidbody.isKinematic = true;
		collider.isTrigger = true;
		transform.position = collision.collider.transform.position;
		rigidbody.isKinematic = false;
		collider.isTrigger = false;

		// Move the controller.
		Vector3 oldPos = _controller.transform.position;
		_controller.transform.position = collision.collider.transform.position;
		Vector3 delta = _controller.transform.position - oldPos;

		// Move the camera.
		Camera.main.transform.Translate(delta, Space.World);
	}
}
