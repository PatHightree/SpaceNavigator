using TDx.TDxInput;
using UnityEngine;
using System.Collections;

public class Yadda : MonoBehaviour {
	private Sensor _sensor;
	private Keyboard _keyboard;
	private Device _device;

	// Use this for initialization
	void Start () {
		_device = new Device();
		_sensor = _device.Sensor;
		_keyboard = _device.Keyboard;

		if (_sensor == null)
			Debug.Log("Sensor is null.");
		if (_keyboard == null)
			Debug.Log("Keyboard is null.");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
