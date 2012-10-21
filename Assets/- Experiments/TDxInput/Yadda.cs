using System.Runtime.InteropServices;
using TDx.TDxInput;
using UnityEngine;

public class Yadda : MonoBehaviour {
	public Vector3 Position;
	public bool IsSensorNull;
	public bool IsDeviceNull;
	public bool IsDeviceConnected;

	private Sensor _sensor;
	//private Keyboard _keyboard;
	private Device _device;

	public void OnEnable() {
		try {
			if (_device == null) {
				_device = new DeviceClass();
				_sensor = _device.Sensor;
				//_keyboard = _device.Keyboard;
				_device.LoadPreferences("Unity");
			}
			if (!_device.IsConnected)
				_device.Connect();
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
		Debug.Log("Initialized");
	}
	
	public void OnDisable() {
		try {
			if (_device != null && _device.IsConnected) {
				_device.Disconnect();
				Debug.Log("Disconnected");
			}
		}
		catch (COMException ex) {
			Debug.Log(ex.ToString());
		}
	}

	public void Update () {
		IsDeviceNull = _device == null;
		IsSensorNull = _sensor == null;
		if (_device != null) IsDeviceConnected = _device.IsConnected;

		if (_device == null || _sensor == null) return;
		if (_device.IsConnected) {
			Position.x = (float) _sensor.Translation.X;
			Position.y = (float) _sensor.Translation.Y;
			Position.z = (float) _sensor.Translation.Z;
		}
	}
}
