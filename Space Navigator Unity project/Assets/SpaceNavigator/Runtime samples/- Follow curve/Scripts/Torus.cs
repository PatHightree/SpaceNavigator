using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Torus : MonoBehaviour {
	private Rigidbody _controller;
	private List<Collider> _colliders;
	private Vector3 _startPos;
	
	public void Awake() {
		_controller = GameObject.FindGameObjectWithTag("Torus controller").GetComponent<Rigidbody>();
		_colliders = GetComponentsInChildren<Collider>().ToList();
		_startPos = transform.position;
	}

	public void OnCollisionEnter(Collision collision) {
		GetComponent<AudioSource>().Play();

		Vector3 resetPos = collision.collider.tag == "Floor" ? _startPos : collision.collider.transform.position;


		// Move the torus.
		GetComponent<Rigidbody>().isKinematic = true;
		_colliders.ForEach(c => c.isTrigger = true);
		transform.position = resetPos;
		GetComponent<Rigidbody>().isKinematic = false;
		_colliders.ForEach(c => c.isTrigger = false);

		// Move the controller.
		Vector3 oldPos = _controller.transform.position;
		_controller.transform.position = resetPos;
		Vector3 delta = _controller.transform.position - oldPos;

		// Move the camera.
		Camera.main.transform.Translate(delta, Space.World);
	}
}
