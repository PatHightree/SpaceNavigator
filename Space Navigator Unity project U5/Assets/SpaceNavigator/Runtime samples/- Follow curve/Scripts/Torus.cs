using UnityEngine;

public class Torus : MonoBehaviour {
	private Rigidbody _controller;
	
	public void Awake() {
		_controller = GameObject.FindGameObjectWithTag("Torus controller").GetComponent<Rigidbody>();
	}

	public void OnCollisionEnter(Collision collision) {
		GetComponent<AudioSource>().Play();

		// Move the torus.
		GetComponent<Rigidbody>().isKinematic = true;
		GetComponent<Collider>().isTrigger = true;
		transform.position = collision.collider.transform.position;
		GetComponent<Rigidbody>().isKinematic = false;
		GetComponent<Collider>().isTrigger = false;

		// Move the controller.
		Vector3 oldPos = _controller.transform.position;
		_controller.transform.position = collision.collider.transform.position;
		Vector3 delta = _controller.transform.position - oldPos;

		// Move the camera.
		Camera.main.transform.Translate(delta, Space.World);
	}
}
