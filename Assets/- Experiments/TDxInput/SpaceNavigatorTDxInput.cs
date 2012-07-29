using System.Runtime.InteropServices;
using UnityEngine;
using TDx.TDxInput;

public class SpaceNavigatorTDxInput : MonoBehaviour {

	private Sensor _sensor;
	private Keyboard _keyboard;
	private Device _device;
	//private System.DateTime _lastUpdate;
	//private bool _isRunning;

	public void Awake () {
		//_isRunning = false;
		try {
			_device = new Device();
			_sensor = _device.Sensor;

			_keyboard = _device.Keyboard;


			//_lastUpdate = System.DateTime.Now;

			//SetDeviceText();
			//SetMotionTexts();
			//SetKeyText(1);
			device_DeviceChange(0);
			// Add the event handlers
			_device.DeviceChange += this.device_DeviceChange;
			_device.DeviceChange += new _ISimpleDeviceEvents_DeviceChangeEventHandler(this.device_DeviceChange);
			_sensor.SensorInput += new _ISensorEvents_SensorInputEventHandler(SetMotionTexts);
			_keyboard.KeyDown += new _IKeyboardEvents_KeyDownEventHandler(keyboard_KeyDown);
			_keyboard.KeyUp += new _IKeyboardEvents_KeyUpEventHandler(keyboard_KeyUp);

			// Associate a configuration with this device'
			//device.LoadPreferences("Unity Editor");

			//Connect everything up
			_device.Connect();

			//device_DeviceChange(0);
		}
		catch (COMException e) {
			Debug.LogError(string.Format("{0} Caught exception #1.", e.Message));
		}
	}

	void SetMotionTexts() {
		double timeFactor = 1;

		//if (false) {
		//	// Adjust values to account for gui update rate
		//	// as this is a velocity device
		//	System.DateTime now = System.DateTime.Now;
		//	System.TimeSpan deltaUpdate = now - lastUpdate;
		//	lastUpdate = now;

		//	timeFactor = (double)deltaUpdate.Milliseconds / sensor.Period;
		//	if (!this.isRunning)
		//		timeFactor = 1;
		//} else {
		//	// In a value monitor the update rate is irrelevant
		//	timeFactor = 1;
		//}

		Vector3D translation;
		translation = _sensor.Translation;
		translation.Length = translation.Length * timeFactor;

		AngleAxis rotation;
		rotation = _sensor.Rotation;
		rotation.Angle = rotation.Angle * timeFactor;

		//if (translation.Length > 0 || rotation.Angle > 0)
		//    _isRunning = true;
		//else
		//    _isRunning = false;

		transform.Translate((float)translation.X, (float)translation.Y, (float)translation.Z);
		transform.RotateAroundLocal(new Vector3((float)rotation.X, (float)rotation.Y, (float)rotation.Z), (float)rotation.Angle );

		System.GC.Collect();
	}

	public void device_DeviceChange(int device) {
		Debug.Log("Device change " + device);
	}
	public void keyboard_KeyDown(int key) {
		Debug.Log("Key down " + key);
	}
	public void keyboard_KeyUp(int key) {
		Debug.Log("Key up " + key);
	}
}
